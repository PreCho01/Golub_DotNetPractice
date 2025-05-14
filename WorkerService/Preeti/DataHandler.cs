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

        // Save data to SQL
        public async Task SaveDataAsync(T data, string tableName)
        {
            using var connection = new SqlConnection(_connStr);
            await connection.OpenAsync();

            // Check if table exists, create if not
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

            // Insert data into the table
            var props = typeof(T).GetProperties();
            var columns = string.Join(",", props.Select(p => p.Name));
            var values = string.Join(",", props.Select(p => "@" + p.Name));
            var query = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

            await connection.ExecuteAsync(query, data);
        }

        // Generate dynamic SQL columns based on the model
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

        // Map C# types to SQL types
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
