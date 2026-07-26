using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper.FluentMap.Conventions;
using Dapper.FluentMap.Diagnostics;
using Dapper.FluentMap.Mapping;
using Dapper.FluentMap.TypeMaps;

namespace Dapper.FluentMap
{
    internal sealed class MappingRegistry
    {
        private readonly ConcurrentDictionary<MappingCacheKey, MappingCacheEntry> _propertyMapCache =
            new ConcurrentDictionary<MappingCacheKey, MappingCacheEntry>();

        internal ConcurrentDictionary<Type, IEntityMap> EntityMaps { get; } =
            new ConcurrentDictionary<Type, IEntityMap>();

        internal ConcurrentDictionary<Type, IList<Convention>> TypeConventions { get; } =
            new ConcurrentDictionary<Type, IList<Convention>>();

        internal int CacheEntryCount => _propertyMapCache.Count;

        internal void AddEntityMap<TEntity>(IEntityMap<TEntity> mapper)
            where TEntity : class
        {
            AddEntityMap(typeof(TEntity), mapper);
        }

        internal void AddEntityMap(Type type, IEntityMap mapper)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            if (EntityMaps.ContainsKey(type))
            {
                throw new FluentMapConfigurationException($"Entity '{type}' already has a configured entity map. Current entity maps: " + string.Join(", ", EntityMaps.Select(e => e.Key.ToString())));
            }

            MappingConfigurationValidator.ValidateEntityMap(type, mapper);
            ValidateIncludedBaseMaps(type, mapper);
            MappingConfigurationValidator.ValidateComposedEntityMap(type, mapper, ComposeExplicitPropertyMaps(type, mapper));

            if (!EntityMaps.TryAdd(type, mapper))
            {
                throw new FluentMapConfigurationException($"Entity '{type}' already has a configured entity map. Current entity maps: " + string.Join(", ", EntityMaps.Select(e => e.Key.ToString())));
            }

            InvalidateType(type);
            SetDapperTypeMap(type);
        }

        internal void AddConvention(Type type, Convention convention)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (convention == null)
            {
                throw new ArgumentNullException(nameof(convention));
            }

            MappingConfigurationValidator.ValidateConvention(type, convention);

            TypeConventions.AddOrUpdate(
                type,
                _ => new List<Convention> { convention },
                (_, current) =>
                {
                    var updated = current.ToList();
                    updated.Add(convention);
                    return updated;
                });

            InvalidateType(type);
            SetDapperTypeMap(type);
        }

        internal void ResetDapperTypeMap<TEntity>()
        {
            SetDapperTypeMap(typeof(TEntity));
        }

        internal void ResetDapperTypeMap(Type type)
        {
            SetDapperTypeMap(type);
        }

        internal PropertyInfo GetFluentPropertyInfo(Type type, string columnName)
        {
            var cacheKey = MappingCacheKey.FluentMap(type, columnName);
            return _propertyMapCache
                .GetOrAdd(cacheKey, _ => new MappingCacheEntry(ResolveFluentPropertyMap(type, columnName)))
                .PropertyInfo;
        }

        internal PropertyInfo GetConventionPropertyInfo(Type type, string columnName)
        {
            var cacheKey = MappingCacheKey.ConventionOnly(type, columnName);
            return _propertyMapCache
                .GetOrAdd(cacheKey, _ => new MappingCacheEntry(ResolveConventionPropertyMap(type, columnName)))
                .PropertyInfo;
        }

        internal IPropertyMap GetFluentPropertyMap(Type type, string columnName)
        {
            var cacheKey = MappingCacheKey.FluentMap(type, columnName);
            return _propertyMapCache
                .GetOrAdd(cacheKey, _ => new MappingCacheEntry(ResolveFluentPropertyMap(type, columnName)))
                .PropertyMap;
        }

        internal IPropertyMap GetConventionPropertyMap(Type type, string columnName)
        {
            var cacheKey = MappingCacheKey.ConventionOnly(type, columnName);
            return _propertyMapCache
                .GetOrAdd(cacheKey, _ => new MappingCacheEntry(ResolveConventionPropertyMap(type, columnName)))
                .PropertyMap;
        }

        internal void ValidateConfiguration()
        {
            var errors = new List<string>();

            foreach (var entityMap in EntityMaps.OrderBy(e => e.Key.FullName))
            {
                try
                {
                    MappingConfigurationValidator.ValidateEntityMap(entityMap.Key, entityMap.Value);
                    ValidateIncludedBaseMaps(entityMap.Key, entityMap.Value);
                    MappingConfigurationValidator.ValidateComposedEntityMap(
                        entityMap.Key,
                        entityMap.Value,
                        ComposeExplicitPropertyMaps(entityMap.Key, entityMap.Value));
                }
                catch (Exception exception)
                {
                    errors.Add(exception.Message);
                }
            }

            foreach (var typeConventions in TypeConventions.OrderBy(c => c.Key.FullName))
            {
                foreach (var convention in typeConventions.Value)
                {
                    try
                    {
                        MappingConfigurationValidator.ValidateConvention(typeConventions.Key, convention);
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception.Message);
                    }
                }
            }

            if (errors.Count == 0)
            {
                return;
            }

            var message = new StringBuilder()
                .Append("Dapper.FluentMap configuration validation found ")
                .Append(errors.Count)
                .Append(errors.Count == 1 ? " error:" : " errors:");

            foreach (var error in errors)
            {
                message.AppendLine().Append("- ").Append(error);
            }

            throw new FluentMapConfigurationException(message.ToString());
        }

        internal MappingExplanation Explain(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
            Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var diagnostics = new List<string>();
            var members = new List<MemberMappingExplanation>();
            var configuredPaths = new List<MemberPath>();
            var entityMapType = default(Type);

            if (EntityMaps.TryGetValue(type, out var entityMap))
            {
                entityMapType = entityMap.GetType();

                foreach (var descriptor in ComposeExplicitPropertyMapDescriptors(type, entityMap))
                {
                    AddMemberExplanation(type, members, configuredPaths, descriptor);
                }
            }

            var conventionTypes = GetConventionTypes(type).ToList();
            foreach (var descriptor in GetConventionPropertyMapDescriptors(type, configuredPaths))
            {
                AddMemberExplanation(type, members, configuredPaths, descriptor);
            }

            AddDapperDefaultExplanations(type, members, configuredPaths);

            if (entityMapType == null && conventionTypes.Count == 0)
            {
                diagnostics.Add("No FluentMap entity map or convention is registered for this entity. Dapper default mapping is used.");
            }

            return new MappingExplanation(
                type,
                entityMapType,
                conventionTypes,
                members.OrderBy(m => m.MemberPath, StringComparer.Ordinal).ThenBy(m => m.ColumnName, StringComparer.Ordinal),
                diagnostics);
        }

        internal void Reset(params Type[] dapperTypes)
        {
            EntityMaps.Clear();
            TypeConventions.Clear();
            _propertyMapCache.Clear();

            if (dapperTypes == null)
            {
                return;
            }

            foreach (var type in dapperTypes)
            {
                SqlMapper.SetTypeMap(type, null);
            }
        }

        private void SetDapperTypeMap(Type type)
        {
            var instance = new FluentMapTypeMap(type);
            SqlMapper.SetTypeMap(type, instance);
        }

        private void InvalidateType(Type type)
        {
            foreach (var key in _propertyMapCache.Keys.Where(k => k.Type == type))
            {
                _propertyMapCache.TryRemove(key, out _);
            }
        }

        private IPropertyMap ResolveFluentPropertyMap(Type type, string columnName)
        {
            var explicitPropertyMaps = GetExplicitPropertyMaps(type);
            var explicitPropertyMap = explicitPropertyMaps.FirstOrDefault(m => MatchColumnNames(m, columnName));

            if (explicitPropertyMap != null)
            {
                return explicitPropertyMap;
            }

            return ResolveConventionPropertyMap(type, columnName, explicitPropertyMaps);
        }

        private IList<IPropertyMap> GetExplicitPropertyMaps(Type type)
        {
            if (EntityMaps.TryGetValue(type, out var entityMap))
            {
                return ComposeExplicitPropertyMaps(type, entityMap);
            }

            return new IPropertyMap[0];
        }

        private IEnumerable<Type> GetConventionTypes(Type type)
        {
            if (!TypeConventions.TryGetValue(type, out var conventions))
            {
                return new Type[0];
            }

            return conventions.Select(c => c.GetType()).ToList();
        }

        private void ValidateIncludedBaseMaps(Type type, IEntityMap entityMap)
        {
            foreach (var baseType in GetIncludedBaseTypes(entityMap))
            {
                if (baseType == type || !baseType.IsClass || !baseType.IsAssignableFrom(type))
                {
                    throw new FluentMapConfigurationException(
                        $"Type '{baseType.FullName}' cannot be included as a base mapping for entity '{type.FullName}'. The included type must be a base class of the entity.");
                }

                if (!EntityMaps.ContainsKey(baseType))
                {
                    throw new FluentMapConfigurationException(
                        $"Entity '{type.FullName}' includes base mapping '{baseType.FullName}', but no entity map has been registered for the base type. Register the base map before the derived map.");
                }
            }
        }

        private IList<IPropertyMap> ComposeExplicitPropertyMaps(Type type, IEntityMap entityMap)
        {
            return ComposeExplicitPropertyMapDescriptors(type, entityMap)
                .Select(d => d.Map)
                .ToList();
        }

        private IList<MappingDiagnosticDescriptor> ComposeExplicitPropertyMapDescriptors(Type type, IEntityMap entityMap)
        {
            var propertyMaps = new List<MappingDiagnosticDescriptor>();
            AddPropertyMapsWithOverride(
                propertyMaps,
                entityMap.PropertyMaps.Select(m => MappingDiagnosticDescriptor.Explicit(m)));

            foreach (var baseType in GetIncludedBaseTypes(entityMap))
            {
                if (!EntityMaps.TryGetValue(baseType, out var baseMap))
                {
                    throw new FluentMapConfigurationException(
                        $"Entity '{type.FullName}' includes base mapping '{baseType.FullName}', but no entity map has been registered for the base type. Register the base map before the derived map.");
                }

                AddPropertyMapsWithOverride(
                    propertyMaps,
                    ComposeExplicitPropertyMapDescriptors(baseType, baseMap)
                        .Select(d => d.AsInheritedFrom(baseType)));
            }

            return propertyMaps;
        }

        private static void AddPropertyMapsWithOverride(IList<MappingDiagnosticDescriptor> target, IEnumerable<MappingDiagnosticDescriptor> maps)
        {
            foreach (var descriptor in maps)
            {
                var memberPath = PropertyMapIdentity.GetMemberPath(descriptor.Map);
                if (target.Any(existingMap => PropertyMapIdentity.GetMemberPath(existingMap.Map).Equals(memberPath)))
                {
                    continue;
                }

                target.Add(descriptor);
            }
        }

        private static IList<Type> GetIncludedBaseTypes(IEntityMap entityMap)
        {
            var mapWithIncludedBases = entityMap as IEntityMapWithIncludedBaseTypes;
            if (mapWithIncludedBases == null)
            {
                return new Type[0];
            }

            return mapWithIncludedBases.IncludedBaseTypes;
        }

        private IEnumerable<MappingDiagnosticDescriptor> GetConventionPropertyMapDescriptors(Type type, IList<MemberPath> configuredPaths)
        {
            if (!TypeConventions.TryGetValue(type, out var conventions))
            {
                yield break;
            }

            foreach (var convention in conventions)
            {
                foreach (var map in convention.PropertyMaps)
                {
                    if (!IsMapForEntity(type, map))
                    {
                        continue;
                    }

                    var memberPath = PropertyMapIdentity.GetMemberPath(map);
                    if (configuredPaths.Any(path => path.Equals(memberPath)))
                    {
                        continue;
                    }

                    yield return MappingDiagnosticDescriptor.Convention(map, convention);
                }
            }
        }

        private void AddDapperDefaultExplanations(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
            Type type,
            IList<MemberMappingExplanation> members,
            IList<MemberPath> configuredPaths)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                         .Where(p => p.GetIndexParameters().Length == 0))
            {
                var memberPath = MemberPath.ForProperty(property);
                if (configuredPaths.Any(path => path.Equals(memberPath)))
                {
                    continue;
                }

                var constructorParameters = GetConstructorParameters(type, property).ToList();
                members.Add(new MemberMappingExplanation(
                    memberPath.ToString(),
                    property,
                    property.Name,
                    MappingSource.DapperDefault,
                    caseSensitive: false,
                    ignored: false,
                    inheritedFrom: null,
                    conventionType: null,
                    constructorParameters: constructorParameters));
                configuredPaths.Add(memberPath);
            }
        }

        private void AddMemberExplanation(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            Type entityType,
            IList<MemberMappingExplanation> members,
            IList<MemberPath> configuredPaths,
            MappingDiagnosticDescriptor descriptor)
        {
            var memberPath = PropertyMapIdentity.GetMemberPath(descriptor.Map);
            var constructorParameters = descriptor.Map.Ignored || memberPath.IsNested
                ? new ConstructorParameterExplanation[0]
                : GetConstructorParameters(entityType, descriptor.Map.PropertyInfo);

            members.Add(new MemberMappingExplanation(
                memberPath.ToString(),
                descriptor.Map.PropertyInfo,
                descriptor.Map.ColumnName,
                descriptor.Source,
                descriptor.Map.CaseSensitive,
                descriptor.Map.Ignored,
                descriptor.InheritedFrom,
                descriptor.ConventionType,
                constructorParameters));
            configuredPaths.Add(memberPath);
        }

        private static IEnumerable<ConstructorParameterExplanation> GetConstructorParameters(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
            Type entityType,
            PropertyInfo property)
        {
            foreach (var constructor in entityType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var parameter = FluentConstructorTypeMap.MatchParameter(constructor.GetParameters(), property.Name);
                if (parameter != null)
                {
                    yield return new ConstructorParameterExplanation(constructor, parameter);
                }
            }
        }

        private IPropertyMap ResolveConventionPropertyMap(Type type, string columnName)
        {
            return ResolveConventionPropertyMap(type, columnName, new IPropertyMap[0]);
        }

        private IPropertyMap ResolveConventionPropertyMap(Type type, string columnName, IList<IPropertyMap> explicitPropertyMaps)
        {
            if (!TypeConventions.TryGetValue(type, out var conventions))
            {
                return null;
            }

            foreach (var convention in conventions)
            {
                var maps = convention.PropertyMaps
#if NETSTANDARD1_3
                                     .Where(map => map.PropertyInfo.DeclaringType == type &&
                                                   !IsExplicitlyMapped(map, explicitPropertyMaps) &&
                                                   MatchColumnNames(map, columnName))
#else
                                     .Where(map => map.PropertyInfo.ReflectedType == type &&
                                                   !IsExplicitlyMapped(map, explicitPropertyMaps) &&
                                                   MatchColumnNames(map, columnName))
#endif
                                     .ToList();

                if (maps.Count > 1)
                {
                    const string msg = "Column '{0}' matched more than one convention property map for entity '{1}' in convention '{2}'. The convention should be more specific.";
                    throw new FluentMapConfigurationException(string.Format(msg, columnName, type, convention.GetType()));
                }

                if (maps.Count == 0)
                {
                    continue;
                }

                return maps[0];
            }

            return null;
        }

        private static bool IsExplicitlyMapped(IPropertyMap conventionMap, IList<IPropertyMap> explicitPropertyMaps)
        {
            var conventionPath = PropertyMapIdentity.GetMemberPath(conventionMap);
            return explicitPropertyMaps.Any(map => PropertyMapIdentity.GetMemberPath(map).Equals(conventionPath));
        }

        private static bool IsMapForEntity(Type type, IPropertyMap map)
        {
#if NETSTANDARD1_3
            return map.PropertyInfo.DeclaringType == type;
#else
            return map.PropertyInfo.ReflectedType == type;
#endif
        }

        private static bool MatchColumnNames(IPropertyMap map, string columnName)
        {
            var comparison = map.CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            return string.Equals(map.ColumnName, columnName, comparison);
        }

        private sealed class MappingCacheEntry
        {
            internal MappingCacheEntry(IPropertyMap propertyMap)
            {
                PropertyMap = propertyMap;

                if (propertyMap == null)
                {
                    return;
                }

                if (!propertyMap.Ignored)
                {
                    PropertyInfo = propertyMap.PropertyInfo;
                    return;
                }

#if !NETSTANDARD1_3
                PropertyInfo = new IgnoredPropertyInfo();
#endif
            }

            internal IPropertyMap PropertyMap { get; }

            internal PropertyInfo PropertyInfo { get; }
        }

        private sealed class MappingDiagnosticDescriptor
        {
            private MappingDiagnosticDescriptor(IPropertyMap map, MappingSource source, Type inheritedFrom, Type conventionType)
            {
                Map = map;
                Source = source;
                InheritedFrom = inheritedFrom;
                ConventionType = conventionType;
            }

            internal IPropertyMap Map { get; }

            internal MappingSource Source { get; }

            internal Type InheritedFrom { get; }

            internal Type ConventionType { get; }

            internal static MappingDiagnosticDescriptor Explicit(IPropertyMap map)
            {
                return new MappingDiagnosticDescriptor(map, MappingSource.Explicit, null, null);
            }

            internal static MappingDiagnosticDescriptor Convention(IPropertyMap map, Convention convention)
            {
                var source = convention is NamingPolicyConvention
                    ? MappingSource.NamingPolicy
                    : MappingSource.Convention;

                return new MappingDiagnosticDescriptor(map, source, null, convention.GetType());
            }

            internal MappingDiagnosticDescriptor AsInheritedFrom(Type baseType)
            {
                return new MappingDiagnosticDescriptor(
                    Map,
                    MappingSource.Inherited,
                    InheritedFrom ?? baseType,
                    ConventionType);
            }
        }
    }
}
