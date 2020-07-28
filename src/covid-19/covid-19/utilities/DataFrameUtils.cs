using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.Analysis;

namespace covid_19.utilities
{
    /// <summary>
    /// Utility functions for DataFrame
    /// Reference: https://gist.github.com/zHaytam/f2a30eeb91b92981e9020dda91ea2697
    /// </summary>
    public static class DataFrameUtils
    {
        public static void PrettyPrint(this DataFrame df)
        {
            var sb = new StringBuilder();
            int width = GetLongestValueLength(df) + 4;

            for (int i = 0; i < df.Columns.Count; i++)
            {
                // Left align by 10
                sb.Append(string.Format(df.Columns[i].Name.PadRight(width)));
            }

            sb.AppendLine();

            long numberOfRows = Math.Min(df.Rows.Count, 25);
            for (int i = 0; i < numberOfRows; i++)
            {
                foreach (object obj in df.Rows[i])
                {
                    sb.Append((obj ?? "null").ToString().PadRight(width));
                }

                sb.AppendLine();
            }

            Console.WriteLine(sb.ToString());
        }

        private static int GetLongestValueLength(DataFrame df)
        {
            long numberOfRows = Math.Min(df.Rows.Count, 25);
            int longestValueLength = 0;

            for (int i = 0; i < numberOfRows; i++)
            {
                foreach (var value in df.Rows[i])
                    longestValueLength = Math.Max(longestValueLength, value?.ToString().Length ?? 0);
            }

            return longestValueLength;
        }

        public static PrimitiveDataFrameColumn<TResult> Apply<T, TResult>(this PrimitiveDataFrameColumn<T> column,
            Func<T, TResult> func)
            where T : unmanaged
            where TResult : unmanaged
        {
            var resultColumn = new PrimitiveDataFrameColumn<TResult>(string.Empty, 0);

            foreach (var row in column)
                resultColumn.Append(func(row.Value));

            return resultColumn;
        }

        /// <summary>
        /// Remove specified columns from a DataFrame
        /// </summary>
        /// <param name="dataFrame">An instance of DataFrame</param>
        /// <param name="toBeRemovedColumnNames">Name of columns to be removed</param>
        public static void RemoveColumns(this DataFrame dataFrame, string[] toBeRemovedColumnNames)
        {
            foreach (var columnName in toBeRemovedColumnNames)
            {
                dataFrame.Columns.Remove(columnName);
            }
        }

        /// <summary>
        /// Remove columns from a DataFrame excluding the specified column names
        /// </summary>
        /// <param name="dataFrame">An instance of DataFrame</param>
        /// <param name="excludedColumnNames">Name of columns to be excluded</param>
        public static void RemoveAllColumnsExcept(this DataFrame dataFrame, string[] excludedColumnNames)
        {
            var columnNames = dataFrame.Columns.Select(col => col.Name).ToArray();
            foreach (var columnName in columnNames)
            {
                if (excludedColumnNames.Contains(columnName) == false)
                {
                    dataFrame.Columns.Remove(columnName);
                }
            }
        }
    }
}
