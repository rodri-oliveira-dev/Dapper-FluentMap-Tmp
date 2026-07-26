using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper.FluentMap.Utils;

namespace Dapper.FluentMap.Mapping
{
    /// <summary>
    /// Represents a non-typed mapping of an entity.
    /// </summary>
    public interface IEntityMap
    {
        /// <summary>
        /// Gets the collection of mapped properties.
        /// </summary>
        IList<IPropertyMap> PropertyMaps { get; }
    }

    /// <summary>
    /// Represents a typed mapping of an entity.
    /// This serves as a marker interface for generic type inference.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to configure the mapping for.</typeparam>
    public interface IEntityMap<TEntity> : IEntityMap
    {
    }

    /// <summary>
    /// Serves as the base class for all entity mapping implementations.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TPropertyMap">The type of the property mapping.</typeparam>
    public abstract class EntityMapBase<TEntity, TPropertyMap> : IEntityMap<TEntity>
        where TPropertyMap : IPropertyMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityMapBase{TEntity, TPropertyMap}"/> class.
        /// </summary>
        protected EntityMapBase()
        {
            PropertyMaps = new List<IPropertyMap>();
        }

        /// <summary>
        /// Gets the collection of mapped properties.
        /// </summary>
        public IList<IPropertyMap> PropertyMaps { get; }

        /// <summary>
        /// Returns an instance of <typeparamref name="TPropertyMap"/> which can perform custom mapping
        /// for the specified property on <typeparamref name="TEntity"/>.
        /// </summary>
        /// <param name="expression">Expression to the property on <typeparamref name="TEntity"/>.</param>
        /// <returns>The created <see cref="T:Dapper.FluentMap.Mapping.PropertyMap"/> instance. This enables a fluent API.</returns>
        /// <exception cref="T:Dapper.FluentMap.FluentMapConfigurationException">when a duplicate mapping is provided.</exception>
        protected TPropertyMap Map(Expression<Func<TEntity, object>> expression)
        {
            var memberPath = ReflectionHelper.GetMemberPath(expression);
            var propertyMap = GetPropertyMap(memberPath.PropertyInfo);
            PropertyMapIdentity.SetMemberPath(propertyMap, memberPath);
            ThrowIfDuplicateMapping(propertyMap);
            PropertyMaps.Add(propertyMap);
            return propertyMap;
        }

        /// <summary>
        /// When overridden in a derived class, gets the property mapping for the specified property.
        /// </summary>
        /// <param name="info">The <see cref="PropertyInfo"/> for the property.</param>
        /// <returns>An instance of <typeparamref name="TPropertyMap"/>.</returns>
        protected abstract TPropertyMap GetPropertyMap(PropertyInfo info);

        private void ThrowIfDuplicateMapping(IPropertyMap map)
        {
            var memberPath = PropertyMapIdentity.GetMemberPath(map);

            if (PropertyMaps.Any(p => PropertyMapIdentity.GetMemberPath(p).Equals(memberPath)))
            {
                var existingMap = PropertyMaps.First(p => PropertyMapIdentity.GetMemberPath(p).Equals(memberPath));
                throw new FluentMapConfigurationException($"Property path '{memberPath}' is already mapped for entity '{typeof(TEntity).FullName}'. Existing column: '{existingMap.ColumnName}'; duplicate column: '{map.ColumnName}'.");
            }
        }
    }

    /// <summary>
    /// Represents a typed mapping of an entity.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity to configure the mapping for.</typeparam>
    public abstract class EntityMap<TEntity> : EntityMapBase<TEntity, PropertyMap>
        where TEntity : class
    {
        /// <inheritdoc />
        protected override PropertyMap GetPropertyMap(PropertyInfo info)
        {
            return new PropertyMap(info);
        }
    }
}
