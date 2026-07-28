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
            var results = new List<TEntity>();
            var materializer = CreateMaterializer<TEntity>(reader, profileType);

            while (reader.Read())
            {
                results.Add(materializer(reader));
            }

            return results;
        }

        internal static Func<IDataRecord, TEntity> CreateMaterializer<
            [DynamicallyAccessedMembers(QueryMappedApiAnnotations.MaterializedEntityMemberTypes)]
            TEntity>(
            IDataRecord reader,
            Type profileType)
            where TEntity : class
        {
            var columnNames = GetColumnNames(reader);

            Func<IDataRecord, object> generatedMaterializer;
            if (FluentMapper.Registry.TryGetGeneratedMaterializer(
                typeof(TEntity),
                profileType,
                columnNames,
                out generatedMaterializer))
            {
                return record => (TEntity)generatedMaterializer(record);
            }

            var plan = FluentMapper.Registry.GetMaterializationPlan(typeof(TEntity), profileType, columnNames);
            return record => (TEntity)plan.Materialize(record);
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
