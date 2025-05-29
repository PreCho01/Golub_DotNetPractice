using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;

namespace WorkerService.Harshit.Services
{
    public class DataHandlerService
    {
        private readonly IDbConnection _dbConnection;
        private readonly ILogger<Worker> _logger;
        public DataHandlerService(IDbConnection dbConnection, ILogger<Worker> logger)
        {
            _dbConnection = dbConnection;
            _logger = logger;
        }
        public async Task<int> AddDataToDb<T>(object data, string tableName)
        {
            await EnsureStoredProcedureExists();

            List<T> dataList = new();

            if (data is T single)
            {
                dataList.Add(single);
            }
            else if (data is IEnumerable<T> list)
            {
                dataList = list.ToList();
            }
            else if (data is JsonElement jsonElement)
            {
                // Handle deserialization from raw JSON input (optional)
                var json = jsonElement.GetRawText();
                if (json.TrimStart().StartsWith("{"))
                {
                    dataList.Add(JsonSerializer.Deserialize<T>(json));
                }
                else
                {
                    dataList = JsonSerializer.Deserialize<List<T>>(json);
                }
            }
            else
            {
                throw new ArgumentException("Input must be a single object or a list of objects.");
            }

            if (!dataList.Any())
                throw new ArgumentException("No data to insert.");

            // Prepare column definitions
            var columnDefs = typeof(T).GetProperties().Select(p => new Dictionary<string, string>
    {
        { "Key", p.Name },
        { "SqlType", GetSqlDbType(p.PropertyType) }
    }).ToList();

            // Prepare row data
            var rows = dataList.Select(item =>
            {
                var dict = new Dictionary<string, string>();
                foreach (var prop in typeof(T).GetProperties())
                {
                    var value = prop.GetValue(item)?.ToString() ?? string.Empty;
                    dict[prop.Name] = value;
                }
                return dict;
            }).ToList();

            var parameters = new DynamicParameters();
            parameters.Add("@TableName", tableName);
            parameters.Add("@Columns", JsonSerializer.Serialize(columnDefs));
            parameters.Add("@Rows", JsonSerializer.Serialize(rows)); // Always an array

            return await _dbConnection.ExecuteAsync("AddData", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task GetDataFromDbOnFile(string filename,string tableName, string ?fileType)
        {
            var result = await _dbConnection.QueryAsync("GetData", new { TableName = tableName});
            await WriteToJsonFileAsync(result, filename);
            
        }
        private async Task<bool> WriteToJsonFileAsync<T>(T data, string fileName, string folder = "SavedFiles")
        {
            if (data == null)
            {
                _logger.LogWarning("Data to serialize is null.");
                return false;
            }

            try
            {
                // Ensure the directory exists
                var directoryPath = Path.Combine(Directory.GetCurrentDirectory(), folder);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                    _logger.LogInformation("Created directory: {Path}", directoryPath);
                }

                var filePath = Path.Combine(directoryPath, fileName);

                // Setup serialization options
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                string json = JsonSerializer.Serialize(data, options);

                // Write to file
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogInformation("Successfully wrote data to {File}", filePath);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access denied when writing to file: {File}", fileName);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error during JSON serialization.");
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "IO error writing to file: {File}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error writing to file.");
            }

            return false;
        }
        private string GetSqlDbType(Type type)
        {
            if (type == typeof(int) || type == typeof(int?)) return "INT";
            if (type == typeof(string)) return "NVARCHAR(MAX)";
            if (type == typeof(DateTime) || type == typeof(DateTime?)) return "DATETIME";
            if (type == typeof(bool) || type == typeof(bool?)) return "BIT";
            if (type == typeof(decimal) || type == typeof(decimal?)) return "DECIMAL(18,2)";
            if (type == typeof(double) || type == typeof(double?)) return "FLOAT";
            // Add more types as needed
            throw new NotSupportedException($"Unsupported property type: {type.Name}");
        }
        public async Task EnsureStoredProcedureExists()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "SqlScripts", "AddDataProcedure.sql");
            var sql = await File.ReadAllTextAsync(path);
            await _dbConnection.ExecuteAsync(sql);
            Console.WriteLine("execx");
        }
    }
}
