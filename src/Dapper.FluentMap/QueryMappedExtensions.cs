using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Dapper.FluentMap.Materialization;
using Dapper.FluentMap.Mapping;

namespace Dapper.FluentMap
{
    /// <summary>
    /// Provides opt-in query helpers for FluentMap-controlled materialization.
    /// </summary>
    public static class QueryMappedExtensions
    {
        private const DynamicallyAccessedMemberTypes MaterializedEntityMemberTypes =
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.PublicProperties;

        private const string QueryMappedRequiresUnreferencedCodeMessage =
            "QueryMapped uses runtime mapping metadata to materialize nested objects. Prefer generated materializers when publishing trimmed or Native AOT applications.";

        private const string QueryMappedRequiresDynamicCodeMessage =
            "QueryMapped compiles runtime accessors for nested object materialization. Prefer generated materializers when publishing Native AOT applications.";

        /// <summary>
        /// Executes a query and materializes rows using FluentMap's opt-in nested object materializer.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to materialize.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="param">Optional query parameters.</param>
        /// <param name="transaction">Optional transaction.</param>
        /// <param name="commandTimeout">Optional command timeout.</param>
        /// <param name="commandType">Optional command type.</param>
        /// <returns>The materialized rows.</returns>
        [RequiresUnreferencedCode(QueryMappedRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedRequiresDynamicCodeMessage)]
        public static IEnumerable<TEntity> QueryMapped<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity>(
            this IDbConnection connection,
            string sql,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
            where TEntity : class
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            if (sql == null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            return QueryMapped<TEntity>(
                connection,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType));
        }

        /// <summary>
        /// Executes a query and materializes rows using the specified FluentMap mapping profile.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to materialize.</typeparam>
        /// <typeparam name="TProfile">The mapping profile marker type to use.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="param">Optional query parameters.</param>
        /// <param name="transaction">Optional transaction.</param>
        /// <param name="commandTimeout">Optional command timeout.</param>
        /// <param name="commandType">Optional command type.</param>
        /// <returns>The materialized rows.</returns>
        [RequiresUnreferencedCode(QueryMappedRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedRequiresDynamicCodeMessage)]
        public static IEnumerable<TEntity> QueryMapped<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity,
            TProfile>(
            this IDbConnection connection,
            string sql,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
            where TEntity : class
            where TProfile : IMappingProfile
        {
            if (sql == null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            return QueryMapped<TEntity, TProfile>(
                connection,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType));
        }

        /// <summary>
        /// Executes a command and materializes rows using FluentMap's opt-in nested object materializer.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to materialize.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>The materialized rows.</returns>
        [RequiresUnreferencedCode(QueryMappedRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedRequiresDynamicCodeMessage)]
        public static IEnumerable<TEntity> QueryMapped<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity>(
            this IDbConnection connection,
            CommandDefinition command)
            where TEntity : class
        {
            return ExecuteMapped<TEntity>(connection, command, profileType: null);
        }

        /// <summary>
        /// Executes a command and materializes rows using the specified FluentMap mapping profile.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to materialize.</typeparam>
        /// <typeparam name="TProfile">The mapping profile marker type to use.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>The materialized rows.</returns>
        [RequiresUnreferencedCode(QueryMappedRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedRequiresDynamicCodeMessage)]
        public static IEnumerable<TEntity> QueryMapped<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity,
            TProfile>(
            this IDbConnection connection,
            CommandDefinition command)
            where TEntity : class
            where TProfile : IMappingProfile
        {
            return ExecuteMapped<TEntity>(connection, command, typeof(TProfile));
        }

        /// <summary>
        /// Executes a query and materializes exactly one row using FluentMap's opt-in nested object materializer.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to materialize.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="param">Optional query parameters.</param>
        /// <param name="transaction">Optional transaction.</param>
        /// <param name="commandTimeout">Optional command timeout.</param>
        /// <param name="commandType">Optional command type.</param>
        /// <returns>The materialized row.</returns>
        [RequiresUnreferencedCode(QueryMappedRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedRequiresDynamicCodeMessage)]
        public static TEntity QueryMappedSingle<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity>(
            this IDbConnection connection,
            string sql,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
            where TEntity : class
        {
            return QueryMapped<TEntity>(connection, sql, param, transaction, commandTimeout, commandType).Single();
        }

        /// <summary>
        /// Executes a query and materializes exactly one row using the specified FluentMap mapping profile.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to materialize.</typeparam>
        /// <typeparam name="TProfile">The mapping profile marker type to use.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="param">Optional query parameters.</param>
        /// <param name="transaction">Optional transaction.</param>
        /// <param name="commandTimeout">Optional command timeout.</param>
        /// <param name="commandType">Optional command type.</param>
        /// <returns>The materialized row.</returns>
        [RequiresUnreferencedCode(QueryMappedRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedRequiresDynamicCodeMessage)]
        public static TEntity QueryMappedSingle<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity,
            TProfile>(
            this IDbConnection connection,
            string sql,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
            where TEntity : class
            where TProfile : IMappingProfile
        {
            return QueryMapped<TEntity, TProfile>(connection, sql, param, transaction, commandTimeout, commandType).Single();
        }

        /// <summary>
        /// Executes a query asynchronously and materializes rows using the specified FluentMap mapping profile.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to materialize.</typeparam>
        /// <typeparam name="TProfile">The mapping profile marker type to use.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="param">Optional query parameters.</param>
        /// <param name="transaction">Optional transaction.</param>
        /// <param name="commandTimeout">Optional command timeout.</param>
        /// <param name="commandType">Optional command type.</param>
        /// <returns>The materialized rows.</returns>
        [RequiresUnreferencedCode(QueryMappedRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedRequiresDynamicCodeMessage)]
        public static Task<IEnumerable<TEntity>> QueryMappedAsync<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity,
            TProfile>(
            this IDbConnection connection,
            string sql,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
            where TEntity : class
            where TProfile : IMappingProfile
        {
            if (sql == null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            return QueryMappedAsync<TEntity, TProfile>(
                connection,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType));
        }

        /// <summary>
        /// Executes a command asynchronously and materializes rows using the specified FluentMap mapping profile.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to materialize.</typeparam>
        /// <typeparam name="TProfile">The mapping profile marker type to use.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>The materialized rows.</returns>
        [RequiresUnreferencedCode(QueryMappedRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedRequiresDynamicCodeMessage)]
        public static Task<IEnumerable<TEntity>> QueryMappedAsync<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity,
            TProfile>(
            this IDbConnection connection,
            CommandDefinition command)
            where TEntity : class
            where TProfile : IMappingProfile
        {
            return ExecuteMappedAsync<TEntity>(connection, command, typeof(TProfile));
        }

        /// <summary>
        /// Executes a query asynchronously and materializes exactly one row using the specified FluentMap mapping profile.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to materialize.</typeparam>
        /// <typeparam name="TProfile">The mapping profile marker type to use.</typeparam>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL query to execute.</param>
        /// <param name="param">Optional query parameters.</param>
        /// <param name="transaction">Optional transaction.</param>
        /// <param name="commandTimeout">Optional command timeout.</param>
        /// <param name="commandType">Optional command type.</param>
        /// <returns>The materialized row.</returns>
        [RequiresUnreferencedCode(QueryMappedRequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedRequiresDynamicCodeMessage)]
        public static async Task<TEntity> QueryMappedSingleAsync<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity,
            TProfile>(
            this IDbConnection connection,
            string sql,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
            where TEntity : class
            where TProfile : IMappingProfile
        {
            var rows = await QueryMappedAsync<TEntity, TProfile>(
                connection,
                sql,
                param,
                transaction,
                commandTimeout,
                commandType).ConfigureAwait(false);

            return rows.Single();
        }

        private static IEnumerable<TEntity> ExecuteMapped<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity>(
            IDbConnection connection,
            CommandDefinition command,
            Type profileType)
            where TEntity : class
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            using (var reader = SqlMapper.ExecuteReader(connection, command))
            {
                return Materialize<TEntity>(reader, profileType);
            }
        }

        private static async Task<IEnumerable<TEntity>> ExecuteMappedAsync<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity>(
            IDbConnection connection,
            CommandDefinition command,
            Type profileType)
            where TEntity : class
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            using (var reader = await SqlMapper.ExecuteReaderAsync(connection, command).ConfigureAwait(false))
            {
                return Materialize<TEntity>(reader, profileType);
            }
        }

        private static IEnumerable<TEntity> Materialize<
            [DynamicallyAccessedMembers(MaterializedEntityMemberTypes)]
            TEntity>(
            IDataReader reader,
            Type profileType)
            where TEntity : class
        {
            var columnNames = GetColumnNames(reader);
            var results = new List<TEntity>();

            Func<IDataRecord, object> generatedMaterializer;
            if (FluentMapper.Registry.TryGetGeneratedMaterializer(
                typeof(TEntity),
                profileType,
                columnNames,
                out generatedMaterializer))
            {
                while (reader.Read())
                {
                    results.Add((TEntity)generatedMaterializer(reader));
                }

                return results;
            }

            var plan = FluentMapper.Registry.GetMaterializationPlan(typeof(TEntity), profileType, columnNames);

            while (reader.Read())
            {
                results.Add((TEntity)plan.Materialize(reader));
            }

            return results;
        }

        private static string[] GetColumnNames(IDataRecord reader)
        {
            var columnNames = new string[reader.FieldCount];
            for (var i = 0; i < columnNames.Length; i++)
            {
                columnNames[i] = reader.GetName(i);
            }

            return columnNames;
        }
    }
}
