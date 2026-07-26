using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapper.FluentMap.Conventions;
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
            var type = typeof(TEntity);
            if (!EntityMaps.TryAdd(type, mapper))
            {
                throw new InvalidOperationException($"Adding entity map for type '{type}' failed. The type already exists. Current entity maps: " + string.Join(", ", EntityMaps.Select(e => e.Key.ToString())));
            }

            InvalidateType(type);
            SetDapperTypeMap(type);
        }

        internal void AddConvention(Type type, Convention convention)
        {
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
                .GetOrAdd(cacheKey, _ => new MappingCacheEntry(ResolveFluentPropertyInfo(type, columnName)))
                .PropertyInfo;
        }

        internal PropertyInfo GetConventionPropertyInfo(Type type, string columnName)
        {
            var cacheKey = MappingCacheKey.ConventionOnly(type, columnName);
            return _propertyMapCache
                .GetOrAdd(cacheKey, _ => new MappingCacheEntry(ResolveConventionPropertyInfo(type, columnName)))
                .PropertyInfo;
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
            var instance = (SqlMapper.ITypeMap)Activator.CreateInstance(typeof(FluentMapTypeMap<>).MakeGenericType(type));
            SqlMapper.SetTypeMap(type, instance);
        }

        private void InvalidateType(Type type)
        {
            foreach (var key in _propertyMapCache.Keys.Where(k => k.Type == type))
            {
                _propertyMapCache.TryRemove(key, out _);
            }
        }

        private PropertyInfo ResolveFluentPropertyInfo(Type type, string columnName)
        {
            var explicitPropertyMaps = GetExplicitPropertyMaps(type);
            var explicitPropertyMap = explicitPropertyMaps.FirstOrDefault(m => MatchColumnNames(m, columnName));

            if (explicitPropertyMap != null)
            {
                if (!explicitPropertyMap.Ignored)
                {
                    return explicitPropertyMap.PropertyInfo;
                }

#if !NETSTANDARD1_3
                return new IgnoredPropertyInfo();
#endif
            }

            return ResolveConventionPropertyInfo(type, columnName, explicitPropertyMaps);
        }

        private IList<IPropertyMap> GetExplicitPropertyMaps(Type type)
        {
            if (EntityMaps.TryGetValue(type, out var entityMap))
            {
                return entityMap.PropertyMaps;
            }

            return new IPropertyMap[0];
        }

        private PropertyInfo ResolveConventionPropertyInfo(Type type, string columnName)
        {
            return ResolveConventionPropertyInfo(type, columnName, new IPropertyMap[0]);
        }

        private PropertyInfo ResolveConventionPropertyInfo(Type type, string columnName, IList<IPropertyMap> explicitPropertyMaps)
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
                    const string msg = "Finding mappings for column '{0}' yielded more than 1 PropertyMap. The conventions should be more specific. Type: '{1}'. Convention: '{2}'.";
                    throw new Exception(string.Format(msg, columnName, type, convention));
                }

                if (maps.Count == 0)
                {
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

        private static bool MatchColumnNames(IPropertyMap map, string columnName)
        {
            var comparison = map.CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            return string.Equals(map.ColumnName, columnName, comparison);
        }

        private sealed class MappingCacheEntry
        {
            internal MappingCacheEntry(PropertyInfo propertyInfo)
            {
                PropertyInfo = propertyInfo;
            }

            internal PropertyInfo PropertyInfo { get; }
        }
    }
}
