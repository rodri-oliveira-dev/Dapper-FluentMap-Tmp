using System;

namespace Dapper.FluentMap.Materialization
{
    /// <summary>
    /// Describes how a generated materializer expects one column in its ordered row shape to map.
    /// </summary>
    public sealed class GeneratedMaterializerColumn
    {
        private GeneratedMaterializerColumn(string columnName, string memberPath, bool ignored)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentException("Column name cannot be null, empty or whitespace.", nameof(columnName));
            }

            if (!ignored && string.IsNullOrWhiteSpace(memberPath))
            {
                throw new ArgumentException("Member path cannot be null, empty or whitespace for a materialized column.", nameof(memberPath));
            }

            ColumnName = columnName;
            MemberPath = memberPath;
            Ignored = ignored;
        }

        /// <summary>
        /// Gets the column name expected at this ordinal.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Gets the mapped member path expected for this column, or <see langword="null"/> for ignored columns.
        /// </summary>
        public string MemberPath { get; }

        /// <summary>
        /// Gets a value indicating whether the current mapping must ignore this column.
        /// </summary>
        public bool Ignored { get; }

        /// <summary>
        /// Creates a descriptor for a materialized column.
        /// </summary>
        /// <param name="columnName">The column name expected at this ordinal.</param>
        /// <param name="memberPath">The member path materialized from the column.</param>
        /// <returns>The generated materializer column descriptor.</returns>
        public static GeneratedMaterializerColumn Map(string columnName, string memberPath)
        {
            return new GeneratedMaterializerColumn(columnName, memberPath, ignored: false);
        }

        /// <summary>
        /// Creates a descriptor for a column that must be ignored by the effective FluentMap configuration.
        /// </summary>
        /// <param name="columnName">The column name expected at this ordinal.</param>
        /// <returns>The generated materializer column descriptor.</returns>
        public static GeneratedMaterializerColumn Ignore(string columnName)
        {
            return new GeneratedMaterializerColumn(columnName, memberPath: null, ignored: true);
        }
    }
}
