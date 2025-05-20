using System.ComponentModel.DataAnnotations;
using CommonHelper;
using Dapper;
using Microsoft.Data.SqlClient;

namespace WorkerService.Preeti
{
    public class DataHandler<T>
    {
        private readonly string _connStr;

        public DataHandler(string connStr)
        {
            _connStr = connStr;
        }

        // Saving data to SQL
        public async Task SaveDataAsync(T data, string tableName)
        {
            using var connection = new SqlConnection(_connStr);
            await connection.OpenAsync();

            // Checking if table exists, and creating if not
            var checkTableQuery = $@"
                IF NOT EXISTS (
                    SELECT * FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = '{tableName}'
                )
                BEGIN
                    CREATE TABLE {tableName} (
                        {GenerateSqlColumns()}
                    )
                END";

            await connection.ExecuteAsync(checkTableQuery);

            var props = typeof(T).GetProperties();
            var whereConditions = new List<string>();
            var parameters = new DynamicParameters();

            foreach (var prop in props)
            {
                var value = prop.GetValue(data);
                if (value == null)
                {
                    whereConditions.Add($"{prop.Name} IS NULL");
                }
                else
                {
                    whereConditions.Add($"{prop.Name} = @{prop.Name}");
                    parameters.Add($"@{prop.Name}", value);
                }
            }

            var whereClause = string.Join(" AND ", whereConditions);
            var checkDuplicateQuery = $"SELECT COUNT(1) FROM {tableName} WHERE {whereClause}";

            var exists = await connection.ExecuteScalarAsync<int>(checkDuplicateQuery, parameters);

            if (exists == 0)
            {
                var columns = string.Join(",", props.Select(p => p.Name));
                var values = string.Join(",", props.Select(p => "@" + p.Name));
                var insertQuery = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

                await connection.ExecuteAsync(insertQuery, data);
                Console.WriteLine("New data inserted into table.");
            }
            else
            {
                Console.WriteLine("Input data is already present in the table. Skipping insert.");

            }

        }

        // Generating dynamic SQL columns based on the model
        private string GenerateSqlColumns()
        {
            var props = typeof(T).GetProperties();
            var columns = new List<string>();

            foreach (var prop in props)
            {
                string columnType = GetSqlType(prop.PropertyType);
                string nullability = prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null ? "NOT NULL" : "NULL";
                columns.Add($"{prop.Name} {columnType} {nullability}");
            }

            return string.Join(",", columns);
        }

        // Mapping C# types to SQL types
        private string GetSqlType(Type type)
        {
            Type t = Nullable.GetUnderlyingType(type) ?? type;

            return t.Name switch
            {
                "Int32" => "INT",
                "Int64" => "BIGINT",
                "Decimal" => "DECIMAL(18,2)",
                "Double" => "FLOAT",
                "DateTime" => "DATETIME",
                "Boolean" => "BIT",
                _ => "NVARCHAR(MAX)"
            };
        }
    }
}
