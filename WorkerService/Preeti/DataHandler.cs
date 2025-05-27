using Dapper;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Collections;
using System.Reflection;

namespace WorkerService.Preeti
{
    public class DataHandler<T>
    {
        private readonly string _connStr;

        public DataHandler(string connStr)
        {
            _connStr = connStr;
        }

        public async Task SaveComplexDataAsync(object data, string tableName)
        {
            using var connection = new SqlConnection(_connStr);
            await connection.OpenAsync();

            var props = data.GetType().GetProperties();
            var columns = new List<string>();
            var values = new List<string>();
            var parameters = new DynamicParameters();

            foreach (var prop in props)
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                string columnName = prop.Name;
                object? value = prop.GetValue(data, null);

                if (value is IEnumerable && !(value is string))
                {
                    // Serialize lists, dictionaries, or other collections
                    string jsonValue = JsonConvert.SerializeObject(value);
                    columns.Add(columnName);
                    values.Add($"@{columnName}");
                    parameters.Add($"@{columnName}", jsonValue);
                }
                else if (IsComplexType(prop.PropertyType))
                {
                    // Serialize nested complex objects
                    string jsonValue = JsonConvert.SerializeObject(value);
                    columns.Add(columnName);
                    values.Add($"@{columnName}");
                    parameters.Add($"@{columnName}", jsonValue);
                }
                else
                {
                    columns.Add(columnName);
                    values.Add($"@{columnName}");
                    parameters.Add($"@{columnName}", value);
                }
            }

            if (columns.Count == 0 || values.Count == 0)
            {
                throw new InvalidOperationException("No valid columns or values found for insertion.");
            }

            // Create table if not exists
            var createTableQuery = $@"
                IF NOT EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = '{tableName}'
                )
                BEGIN
                    CREATE TABLE {tableName} (
                        {string.Join(",", columns.Select(c => $"{c} NVARCHAR(MAX) NULL"))}
                    )
                END";

            await connection.ExecuteAsync(createTableQuery);

            // Insert data
            var insertQuery = $"INSERT INTO {tableName} ({string.Join(",", columns)}) VALUES ({string.Join(",", values)})";
            await connection.ExecuteAsync(insertQuery, parameters);
        }

        private bool IsComplexType(Type type)
        {
            return !(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal));
        }
    }
}
