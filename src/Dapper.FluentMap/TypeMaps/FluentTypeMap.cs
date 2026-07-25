using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapper.FluentMap.Conventions;
using Dapper.FluentMap.Mapping;

namespace Dapper.FluentMap.TypeMaps
{
    /// <summary>
    /// Represents a Dapper type mapping strategy which first tries explicit fluent mappings,
    /// then configured conventions, and finally the <see cref="T:Dapper.DefaultTypeMap"/>.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    public class FluentMapTypeMap<TEntity> : MultiTypeMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Dapper.FluentMap.TypeMaps.FluentTypeMap"/> class
        /// which uses explicit fluent mappings, conventions and <see cref="T:Dapper.DefaultTypeMap"/>
        /// as mapping strategies.
        /// </summary>
        public FluentMapTypeMap()
            : base(new CustomPropertyTypeMap(typeof(TEntity), GetPropertyInfo), new DefaultTypeMap(typeof(TEntity)))
        {
        }

        private static PropertyInfo GetPropertyInfo(Type type, string columnName)
        {
            var cacheKey = $"FluentMapTypeMap;{type.FullName};{columnName}";

            PropertyInfo info;
            if (TypePropertyMapCache.TryGetValue(cacheKey, out info))
            {
                return info;
            }

            var explicitPropertyMaps = GetExplicitPropertyMaps(type);
            var explicitPropertyMap = explicitPropertyMaps.FirstOrDefault(m => MatchColumnNames(m, columnName));

            if (explicitPropertyMap != null)
            {
                if (!explicitPropertyMap.Ignored)
                {
                    TypePropertyMapCache.TryAdd(cacheKey, explicitPropertyMap.PropertyInfo);
                    return explicitPropertyMap.PropertyInfo;
                }
#if !NETSTANDARD1_3
                else
                {
                    var ignoredPropertyInfo = new IgnoredPropertyInfo();
                    TypePropertyMapCache.TryAdd(cacheKey, ignoredPropertyInfo);
                    return ignoredPropertyInfo;
                }
#endif
            }

            info = GetConventionPropertyInfo(type, columnName, explicitPropertyMaps);
            if (info != null)
            {
                TypePropertyMapCache.TryAdd(cacheKey, info);
                return info;
            }

            // If we get here, the property was not mapped.
            TypePropertyMapCache.TryAdd(cacheKey, null);
            return null;
        }

        private static IList<IPropertyMap> GetExplicitPropertyMaps(Type type)
        {
            IEntityMap entityMap;
            if (FluentMapper.EntityMaps.TryGetValue(type, out entityMap))
            {
                return entityMap.PropertyMaps;
            }

            return new IPropertyMap[0];
        }

        private static PropertyInfo GetConventionPropertyInfo(Type type, string columnName, IList<IPropertyMap> explicitPropertyMaps)
        {
            IList<Convention> conventions;
            if (!FluentMapper.TypeConventions.TryGetValue(type, out conventions))
            {
                return null;
            }

            foreach (var convention in conventions)
            {
                // Find property map for current type and column name.
                var maps = convention.PropertyMaps
#if NETSTANDARD1_3
                                     // HACK: ReflectedType isn't available on.NET Standard 1.3,
                                     // this will cause issues when mapping derived entities.
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
                    const string msg = "Finding mappings for column '{0}' yielded more than 1 PropertyMap. The conventions should be more specific. Type: '{1}'. Convention: '{2}'.";
                    throw new Exception(string.Format(msg, columnName, type, convention));
                }

                if (maps.Count == 0)
                {
                    // This convention has no property maps, continue to next convention.
                    continue;
                }

                return maps[0].PropertyInfo;
            }

            return null;
        }

        private static bool IsExplicitlyMapped(IPropertyMap conventionMap, IList<IPropertyMap> explicitPropertyMaps)
        {
            return explicitPropertyMaps.Any(map => map.PropertyInfo.Name == conventionMap.PropertyInfo.Name);
        }
    }
}
