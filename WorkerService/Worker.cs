using System.Diagnostics;
using System.Text.Json;
using CommonHelper;
using WorkerService.Models;
using WorkerService.Preeti;

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private IConfigSetting _config;
        public Worker(ILogger<Worker> logger, IConfigSetting config)
        {
            _logger = logger;
            _config = config;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connStr = _config.GetAppSettings("DefaultConnection", "ConnectionStrings");

            await ProcessJsonFileAsync<Employee>("empData.json", "Employee", connStr);

        }

        private async Task ProcessJsonFileAsync<T>(string fileName, string tableName, string connStr)
        {
            string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "InputFiles", fileName);

            if (File.Exists(jsonFilePath))
            {
                try
                {
                    var jsonData = await File.ReadAllTextAsync(jsonFilePath);

                    var model = JsonSerializer.Deserialize<T>(jsonData);

                    var handler = new DataHandler<T>(connStr);
                    await handler.SaveDataAsync(model, tableName);

                    string saveFolder = Path.Combine(Directory.GetCurrentDirectory(), "SavedJsons");
                    await SaveInFile.SaveJsonToFileAsync(model, saveFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_output.json");

                    _logger.LogInformation($"Processed {fileName} into {tableName}");
                }

                catch (Exception ex)
                {
                    _logger.LogError($"Error:{ex.Message}");
                }
            }
            else
            {
                _logger.LogError($"File {jsonFilePath} does not exist.");
            }

            _logger.LogInformation("File Processed completed.");

        }
    }
}
