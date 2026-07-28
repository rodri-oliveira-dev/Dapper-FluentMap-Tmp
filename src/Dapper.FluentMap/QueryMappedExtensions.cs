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
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static IEnumerable<TEntity> QueryMapped<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static IEnumerable<TEntity> QueryMapped<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static IEnumerable<TEntity> QueryMapped<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static IEnumerable<TEntity> QueryMapped<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static TEntity QueryMappedSingle<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static TEntity QueryMappedSingle<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static Task<IEnumerable<TEntity>> QueryMappedAsync<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static Task<IEnumerable<TEntity>> QueryMappedAsync<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static async Task<TEntity> QueryMappedSingleAsync<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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

        /// <summary>
        /// Executes a query and returns a reader for sequential FluentMap-controlled materialization of multiple result sets.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="sql">The SQL command to execute.</param>
        /// <param name="param">Optional query parameters.</param>
        /// <param name="transaction">Optional transaction.</param>
        /// <param name="commandTimeout">Optional command timeout.</param>
        /// <param name="commandType">Optional command type.</param>
        /// <returns>A disposable mapped multiple result reader.</returns>
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static MappedGridReader QueryMultipleMapped(
            this IDbConnection connection,
            string sql,
            object param = null,
            IDbTransaction transaction = null,
            int? commandTimeout = null,
            CommandType? commandType = null)
        {
            if (sql == null)
            {
                throw new ArgumentNullException(nameof(sql));
            }

            return QueryMultipleMapped(
                connection,
                new CommandDefinition(sql, param, transaction, commandTimeout, commandType));
        }

        /// <summary>
        /// Executes a command and returns a reader for sequential FluentMap-controlled materialization of multiple result sets.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="command">The command to execute.</param>
        /// <returns>A disposable mapped multiple result reader.</returns>
        [RequiresUnreferencedCode(QueryMappedApiAnnotations.RequiresUnreferencedCodeMessage)]
        [RequiresDynamicCode(QueryMappedApiAnnotations.RequiresDynamicCodeMessage)]
        public static MappedGridReader QueryMultipleMapped(
            this IDbConnection connection,
            CommandDefinition command)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            return new MappedGridReader(SqlMapper.ExecuteReader(connection, command));
        }

        private static IEnumerable<TEntity> ExecuteMapped<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
                return MappedRowMaterializer.Materialize<TEntity>(reader, profileType);
            }
        }

        private static async Task<IEnumerable<TEntity>> ExecuteMappedAsync<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
                return MappedRowMaterializer.Materialize<TEntity>(reader, profileType);
            }
        }
    }
}
