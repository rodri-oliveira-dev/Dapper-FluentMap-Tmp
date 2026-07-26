using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper.FluentMap.Mapping;

namespace Dapper.FluentMap.Materialization
{
    internal sealed class NestedMaterializationPlan
    {
        private readonly Func<object> _entityFactory;
        private readonly Assignment[] _rootAssignments;
        private readonly NestedNode[] _nestedNodes;

        private NestedMaterializationPlan(
            Func<object> entityFactory,
            IEnumerable<Assignment> rootAssignments,
            IEnumerable<NestedNode> nestedNodes)
        {
            _entityFactory = entityFactory;
            _rootAssignments = rootAssignments.ToArray();
            _nestedNodes = nestedNodes.ToArray();
        }

        internal static NestedMaterializationPlan Create(Type entityType, IReadOnlyList<string> columnNames, MappingRegistry registry)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            if (columnNames == null)
            {
                throw new ArgumentNullException(nameof(columnNames));
            }

            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            var entityFactory = CreateFactory(entityType, $"Entity type '{FormatType(entityType)}' must have a public parameterless constructor to use QueryMapped when nested mappings are present.");
            var defaultTypeMap = new DefaultTypeMap(entityType);
            var rootAssignments = new List<Assignment>();
            var nestedNodes = new List<NestedNode>();

            for (var i = 0; i < columnNames.Count; i++)
            {
                var columnName = columnNames[i];
                var fluentMap = registry.GetFluentPropertyMap(entityType, columnName);
                if (fluentMap != null)
                {
                    if (fluentMap.Ignored)
                    {
                        continue;
                    }

                    var memberPath = PropertyMapIdentity.GetMemberPath(fluentMap);
                    if (memberPath.IsNested)
                    {
                        AddNestedAssignment(nestedNodes, memberPath, i);
                        continue;
                    }

                    rootAssignments.Add(Assignment.ForProperty(i, memberPath.PropertyInfo));
                    continue;
                }

                var defaultMember = defaultTypeMap.GetMember(columnName);
                if (defaultMember == null)
                {
                    continue;
                }

                if (defaultMember.Property != null)
                {
                    rootAssignments.Add(Assignment.ForProperty(i, defaultMember.Property));
                }
                else if (defaultMember.Field != null)
                {
                    rootAssignments.Add(Assignment.ForField(i, defaultMember.Field));
                }
            }

            foreach (var node in nestedNodes)
            {
                node.Seal();
            }

            return new NestedMaterializationPlan(entityFactory, rootAssignments, nestedNodes);
        }

        internal object Materialize(IDataRecord record)
        {
            var entity = _entityFactory();

            foreach (var assignment in _rootAssignments)
            {
                assignment.Assign(entity, record);
            }

            foreach (var node in _nestedNodes)
            {
                node.Apply(entity, record);
            }

            return entity;
        }

        private static void AddNestedAssignment(IList<NestedNode> rootNodes, MemberPath memberPath, int columnIndex)
        {
            var properties = memberPath.Properties;
            var nodes = rootNodes;
            var node = default(NestedNode);

            for (var i = 0; i < properties.Count - 1; i++)
            {
                node = FindOrAddNode(nodes, properties[i]);
                nodes = node.Children;
            }

            node.Leaves.Add(Assignment.ForProperty(columnIndex, properties[properties.Count - 1]));
        }

        private static NestedNode FindOrAddNode(IList<NestedNode> nodes, PropertyInfo property)
        {
            var node = nodes.FirstOrDefault(n => Equals(n.Property, property));
            if (node != null)
            {
                return node;
            }

            node = new NestedNode(property);
            nodes.Add(node);
            return node;
        }

        private static Func<object> CreateFactory(Type type, string errorMessage)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                throw new FluentMapConfigurationException(errorMessage);
            }

            var body = Expression.Convert(Expression.New(constructor), typeof(object));
            return Expression.Lambda<Func<object>>(body).Compile();
        }

        private static Func<object, object> CreateGetter(PropertyInfo property)
        {
            var target = Expression.Parameter(typeof(object), "target");
            var body = Expression.Convert(
                Expression.Property(Expression.Convert(target, property.DeclaringType), property),
                typeof(object));

            return Expression.Lambda<Func<object, object>>(body, target).Compile();
        }

        private static Action<object, object> CreatePropertySetter(PropertyInfo property)
        {
            var target = Expression.Parameter(typeof(object), "target");
            var value = Expression.Parameter(typeof(object), "value");
            var body = Expression.Assign(
                Expression.Property(Expression.Convert(target, property.DeclaringType), property),
                Expression.Convert(value, property.PropertyType));

            return Expression.Lambda<Action<object, object>>(body, target, value).Compile();
        }

        private static Action<object, object> CreateFieldSetter(FieldInfo field)
        {
            var target = Expression.Parameter(typeof(object), "target");
            var value = Expression.Parameter(typeof(object), "value");
            var body = Expression.Assign(
                Expression.Field(Expression.Convert(target, field.DeclaringType), field),
                Expression.Convert(value, field.FieldType));

            return Expression.Lambda<Action<object, object>>(body, target, value).Compile();
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null || value == DBNull.Value)
            {
                return GetDefaultValue(targetType);
            }

            var conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (conversionType.IsInstanceOfType(value))
            {
                return value;
            }

            if (conversionType.GetTypeInfo().IsEnum)
            {
                return value is string text
                    ? Enum.Parse(conversionType, text)
                    : Enum.ToObject(conversionType, value);
            }

            if (conversionType == typeof(Guid) && value is string guidText)
            {
                return new Guid(guidText);
            }

            return Convert.ChangeType(value, conversionType, CultureInfo.InvariantCulture);
        }

        private static object GetDefaultValue(Type type)
        {
            if (!type.GetTypeInfo().IsValueType || Nullable.GetUnderlyingType(type) != null)
            {
                return null;
            }

            return Activator.CreateInstance(type);
        }

        private static bool CanAssignNull(Type type)
        {
            return !type.GetTypeInfo().IsValueType || Nullable.GetUnderlyingType(type) != null;
        }

        private static bool HasNonNullValue(IDataRecord record, IEnumerable<int> columnIndexes)
        {
            return columnIndexes.Any(index => !record.IsDBNull(index));
        }

        private static string FormatType(Type type)
        {
            return type == null ? "<unknown>" : type.FullName;
        }

        private sealed class NestedNode
        {
            private int[] _subtreeColumnIndexes;

            internal NestedNode(PropertyInfo property)
            {
                Property = property;
                Children = new List<NestedNode>();
                Leaves = new List<Assignment>();
                Getter = CreateGetter(property);
                Setter = CreatePropertySetter(property);
                Factory = CreateFactory(
                    property.PropertyType,
                    $"Intermediate property '{property.Name}' of type '{FormatType(property.PropertyType)}' must have a public parameterless constructor for nested materialization.");
            }

            internal PropertyInfo Property { get; }

            internal IList<NestedNode> Children { get; }

            internal IList<Assignment> Leaves { get; }

            private Func<object, object> Getter { get; }

            private Action<object, object> Setter { get; }

            private Func<object> Factory { get; }

            internal void Seal()
            {
                foreach (var child in Children)
                {
                    child.Seal();
                }

                _subtreeColumnIndexes = Leaves
                    .Select(leaf => leaf.ColumnIndex)
                    .Concat(Children.SelectMany(child => child._subtreeColumnIndexes))
                    .Distinct()
                    .ToArray();
            }

            internal void Apply(object parent, IDataRecord record)
            {
                if (!HasNonNullValue(record, _subtreeColumnIndexes))
                {
                    if (CanAssignNull(Property.PropertyType))
                    {
                        Setter(parent, null);
                    }

                    return;
                }

                var current = Getter(parent);
                if (current == null)
                {
                    current = Factory();
                    Setter(parent, current);
                }

                foreach (var child in Children)
                {
                    child.Apply(current, record);
                }

                foreach (var leaf in Leaves)
                {
                    leaf.Assign(current, record);
                }
            }
        }

        private sealed class Assignment
        {
            private readonly Type _targetType;
            private readonly Action<object, object> _setter;

            private Assignment(int columnIndex, Type targetType, Action<object, object> setter)
            {
                ColumnIndex = columnIndex;
                _targetType = targetType;
                _setter = setter;
            }

            internal int ColumnIndex { get; }

            internal static Assignment ForProperty(int columnIndex, PropertyInfo property)
            {
                return new Assignment(columnIndex, property.PropertyType, CreatePropertySetter(property));
            }

            internal static Assignment ForField(int columnIndex, FieldInfo field)
            {
                return new Assignment(columnIndex, field.FieldType, CreateFieldSetter(field));
            }

            internal void Assign(object target, IDataRecord record)
            {
                _setter(target, ConvertValue(record.GetValue(ColumnIndex), _targetType));
            }
        }
    }
}
