using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace Dapper.FluentMap.Materialization
{
    internal static class MappedRowMaterializer
    {
        internal static IEnumerable<TEntity> Materialize<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
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
