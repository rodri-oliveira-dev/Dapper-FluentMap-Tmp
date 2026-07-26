using System;
using System.Reflection;
using Dapper.FluentMap.Mapping;

namespace Dapper.FluentMap.TypeMaps
{
    internal sealed class FluentMapTypeMap : MultiTypeMap
    {
        internal FluentMapTypeMap(Type entityType)
            : base(
                new FluentConstructorTypeMap(entityType, GetPropertyMap),
                new CustomPropertyTypeMap(entityType, GetPropertyInfo),
                new DefaultTypeMap(entityType))
        {
        }

        private static IPropertyMap GetPropertyMap(Type type, string columnName)
        {
            return FluentMapper.Registry.GetFluentPropertyMap(type, columnName);
        }

        private static PropertyInfo GetPropertyInfo(Type type, string columnName)
        {
            return FluentMapper.Registry.GetFluentPropertyInfo(type, columnName);
        }
    }
}
