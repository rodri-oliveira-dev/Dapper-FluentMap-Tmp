using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dapper.FluentMap.Configuration;
using Dapper.FluentMap.Conventions;
using Dapper.FluentMap.Diagnostics;
using Dapper.FluentMap.Mapping;

namespace Dapper.FluentMap
{
    /// <summary>
    /// Main entry point for Dapper.FluentMap configuration.
    /// </summary>
    public static class FluentMapper
    {
        private const DynamicallyAccessedMemberTypes EntityMemberTypes =
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.PublicProperties;

        private static readonly MappingRegistry _registry = new MappingRegistry();
        private static readonly FluentMapConfiguration _configuration = new FluentMapConfiguration();

        /// <summary>
        /// Gets the dictionary containing the entity mapping per entity type.
        /// </summary>
        /// <remarks>
        /// This mutable dictionary is preserved for source and binary compatibility. Prefer configuring maps
        /// through <see cref="Initialize(Action{FluentMapConfiguration})"/> and use <see cref="GetEntityMaps"/>
        /// for read-only inspection.
        /// </remarks>
        public static readonly ConcurrentDictionary<Type, IEntityMap> EntityMaps = _registry.EntityMaps;

        /// <summary>
        /// Gets the dictionary containing the conventions per entity type.
        /// </summary>
        /// <remarks>
        /// This mutable dictionary is preserved for source and binary compatibility. Prefer configuring conventions
        /// through <see cref="Initialize(Action{FluentMapConfiguration})"/> and use <see cref="GetTypeConventions"/>
        /// for read-only inspection.
        /// </remarks>
        public static readonly ConcurrentDictionary<Type, IList<Convention>> TypeConventions = _registry.TypeConventions;

        internal static MappingRegistry Registry => _registry;

        /// <summary>
        /// Initializes Dapper.FluentMap with the specified configuration.
        /// This is method should be called when the application starts or when the first mapping is needed.
        /// </summary>
        /// <param name="configure">A callback containing the configuration of Dapper.FluentMap.</param>
        public static void Initialize(Action<FluentMapConfiguration> configure)
        {
            configure(_configuration);
        }

        /// <summary>
        /// Validates the current Dapper.FluentMap configuration.
        /// </summary>
        /// <exception cref="T:Dapper.FluentMap.FluentMapConfigurationException">
        /// when one or more configuration errors are found.
        /// </exception>
        public static void Validate()
        {
            _registry.ValidateConfiguration();
        }

        /// <summary>
        /// Gets a read-only snapshot of the default entity maps currently registered in Dapper.FluentMap.
        /// </summary>
        /// <returns>A read-only snapshot of the registered default entity maps.</returns>
        public static IReadOnlyDictionary<Type, IEntityMap> GetEntityMaps()
        {
            return _registry.GetEntityMapsSnapshot();
        }

        /// <summary>
        /// Gets a read-only snapshot of the type conventions currently registered in Dapper.FluentMap.
        /// </summary>
        /// <returns>A read-only snapshot of the registered type conventions.</returns>
        public static IReadOnlyDictionary<Type, IReadOnlyList<Convention>> GetTypeConventions()
        {
            return _registry.GetTypeConventionsSnapshot();
        }

        /// <summary>
        /// Explains the effective mapping configuration for the specified entity type.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to explain.</typeparam>
        /// <returns>A structured explanation of configured mappings, conventions and fallback mappings.</returns>
        public static MappingExplanation Explain<
            [DynamicallyAccessedMembers(EntityMemberTypes)]
            TEntity>()
        {
            return _registry.Explain(typeof(TEntity));
        }

        /// <summary>
        /// Explains the effective mapping configuration for the specified entity type and mapping profile.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to explain.</typeparam>
        /// <typeparam name="TProfile">The mapping profile marker type to explain.</typeparam>
        /// <returns>A structured explanation of configured mappings, conventions and fallback mappings.</returns>
        public static MappingExplanation Explain<
            [DynamicallyAccessedMembers(EntityMemberTypes)]
            TEntity,
            TProfile>()
            where TProfile : IMappingProfile
        {
            return _registry.Explain(typeof(TEntity), typeof(TProfile));
        }

        /// <summary>
        /// Registers a Dapper type map using fluent mapping for the specified <typeparamref name="TEntity"/>.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        internal static void AddTypeMap<TEntity>()
        {
            _registry.ResetDapperTypeMap<TEntity>();
        }

        /// <summary>
        /// Registers a Dapper type map using fluent mapping for the specified <paramref name="entityType"/>.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        internal static void AddTypeMap(Type entityType)
        {
            _registry.ResetDapperTypeMap(entityType);
        }

        /// <summary>
        /// Registers a Dapper type map using conventions for the specified <typeparamref name="TEntity"/>.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        internal static void AddConventionTypeMap<TEntity>()
        {
            AddTypeMap<TEntity>();
        }

        /// <summary>
        /// Registers a Dapper type map using conventions for the specified <paramref name="entityType"/>.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        internal static void AddConventionTypeMap(Type entityType)
        {
            AddTypeMap(entityType);
        }

        internal static void Reset(params Type[] dapperTypes)
        {
            _registry.Reset(dapperTypes);
        }
    }
}
