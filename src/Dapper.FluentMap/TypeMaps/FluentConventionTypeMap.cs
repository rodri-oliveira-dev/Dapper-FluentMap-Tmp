using System;
using System.Reflection;
using Dapper.FluentMap.Mapping;

namespace Dapper.FluentMap.TypeMaps
{
    /// <summary>
    /// Represents a Dapper type mapping strategy which first tries to map the type using a
    /// <see cref="T:Dapper.CustomPropertyTypeMap"/>
    /// with the configured conventions. <see cref="T:Dapper.DefaultTypeMap"/> is used as fallback mapping strategy.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class FluentConventionTypeMap<TEntity> : MultiTypeMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Dapper.FluentMap.TypeMaps.FluentConventionTypeMap"/> class
        /// which uses the <see cref="T:Dapper.CustomPropertyTypeMap"/> and <see cref="T:Dapper.DefaultTypeMap"/>
        /// as mapping strategies.
        /// </summary>
        public FluentConventionTypeMap()
            : base(
                new FluentConstructorTypeMap(typeof(TEntity), GetPropertyMap),
                new CustomPropertyTypeMap(typeof(TEntity), GetPropertyInfo),
                new DefaultTypeMap(typeof(TEntity)))
        {
        }

        private static IPropertyMap GetPropertyMap(Type type, string columnName)
        {
            return FluentMapper.Registry.GetConventionPropertyMap(type, columnName);
        }

        private static PropertyInfo GetPropertyInfo(Type type, string columnName)
        {
            return FluentMapper.Registry.GetConventionPropertyInfo(type, columnName);
        }
    }
}
