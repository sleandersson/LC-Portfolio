using System;
using System.Data;
using System.Text;
using Microsoft.Data.Sqlite;

namespace LC_Portfolio.Database
{
    public static class DatabaseHelper
    {
        // Generates SQL for creating a table based on the DataTable structure.
        public static string GenerateCreateTableSql(DataTable dataTable)
        {
            var sb = new StringBuilder();
            sb.Append($"CREATE TABLE IF NOT EXISTS \"{dataTable.TableName}\" (");

            foreach (DataColumn column in dataTable.Columns)
            {
                sb.Append($"\"{column.ColumnName}\" TEXT,"); // Use double quotes for column names
            }

            if (dataTable.Columns.Count > 0)
            {
                sb.Length--; // Correctly remove the last comma if columns exist
            }
            sb.Append(");");
            return sb.ToString();
        }
        // Generates SQL for inserting a row into the table based on the DataRow content.
        public static void GenerateInsertSql(DataTable dataTable, DataRow row, SqliteCommand command)
        {
            command.Parameters.Clear(); // Clear existing parameters before adding new ones
            // Lists to hold column names and parameter placeholders
            var columnNames = new List<string>();
            var paramPlaceholders = new List<string>();

            // Iterate through all columns in the DataTable
            foreach (DataColumn column in dataTable.Columns)
            {
                // Use column name directly since it's already sanitized
                string columnName = column.ColumnName;
                // Create a parameter placeholder using the column name
                string paramPlaceholder = $"@{columnName}";

                // Add the column name and parameter placeholder to their respective lists
                columnNames.Add(columnName);
                paramPlaceholders.Add(paramPlaceholder);

                // Add the parameter to the command object
                if (row[column] != null && row[column] != DBNull.Value)
                {
                    command.Parameters.AddWithValue(paramPlaceholder, row[column]);
                }
                else
                {
                    // If the value is null, use DBNull.Value to represent a null value in the database
                    command.Parameters.AddWithValue(paramPlaceholder, DBNull.Value);
                }
            }
            // Join the column names and parameter placeholders into comma-separated strings
            string columnsPart = string.Join(", ", columnNames.Select(name => $"\"{name}\"")); // Ensure column names are quoted
            string valuesPart = string.Join(", ", paramPlaceholders);
            // Construct the INSERT INTO command text
            command.CommandText = $"INSERT INTO \"{dataTable.TableName}\" ({columnsPart}) VALUES ({valuesPart});";
        }
    }
}
