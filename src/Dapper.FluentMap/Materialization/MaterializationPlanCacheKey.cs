using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dapper.FluentMap.Materialization
{
    internal sealed class MaterializationPlanCacheKey : IEquatable<MaterializationPlanCacheKey>
    {
        private readonly string[] _columnNames;
        private readonly int _hashCode;

        internal MaterializationPlanCacheKey(Type type, IEnumerable<string> columnNames)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (columnNames == null)
            {
                throw new ArgumentNullException(nameof(columnNames));
            }

            Type = type;
            _columnNames = columnNames.ToArray();
            ColumnNames = new ReadOnlyCollection<string>(_columnNames);
            _hashCode = CalculateHashCode(type, _columnNames);
        }

        internal Type Type { get; }

        internal IReadOnlyList<string> ColumnNames { get; }

        public bool Equals(MaterializationPlanCacheKey other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other == null || Type != other.Type || _columnNames.Length != other._columnNames.Length)
            {
                return false;
            }

            for (var i = 0; i < _columnNames.Length; i++)
            {
                if (!string.Equals(_columnNames[i], other._columnNames[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is MaterializationPlanCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }

        private static int CalculateHashCode(Type type, string[] columnNames)
        {
            unchecked
            {
                var hash = type.GetHashCode();
                foreach (var columnName in columnNames)
                {
                    hash = (hash * 31) + (columnName == null ? 0 : StringComparer.Ordinal.GetHashCode(columnName));
                }

                return hash;
            }
        }
    }
}
