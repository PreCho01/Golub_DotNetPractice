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
        public async Task<int> AddDataToDb<T>(T data,string tableName)
        {
            var parameters = new DynamicParameters();
            foreach(var prop in typeof(T).GetProperties())
            {
                parameters.Add(prop.Name, prop.GetValue(data));
            }
            parameters.Add("TableName",tableName);
            var result = await _dbConnection.ExecuteAsync("AddData",parameters);
            return result;
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
    }
}
