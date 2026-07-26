using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dapper.FluentMap.Materialization;

namespace Dapper.FluentMap
{
    /// <summary>
    /// Provides opt-in query helpers for FluentMap-controlled materialization.
    /// </summary>
    public static class QueryMappedExtensions
    {
        private const DynamicallyAccessedMemberTypes MaterializedEntityMemberTypes =
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
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

            using (var reader = SqlMapper.ExecuteReader(connection, sql, param, transaction, commandTimeout, commandType))
            {
                var columnNames = GetColumnNames(reader);
                var plan = FluentMapper.Registry.GetMaterializationPlan(typeof(TEntity), columnNames);
                var results = new List<TEntity>();

                while (reader.Read())
                {
                    results.Add((TEntity)plan.Materialize(reader));
                }

                return results;
            }
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
