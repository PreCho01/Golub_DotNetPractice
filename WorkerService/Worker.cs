using System.Diagnostics;
using System.Text.Json;
using CommonHelper;
using WorkerService.Harshit.Services;
using WorkerService.Models;
using WorkerService.Preeti;

namespace WorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private IConfigSetting _config;
        private readonly DataHandlerService _dataHandlerService;
        public Worker(ILogger<Worker> logger, IConfigSetting config,DataHandlerService dataHandlerService)
        {
            _logger = logger;
            _config = config;
            _dataHandlerService = dataHandlerService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connStr = _config.GetAppSettings("DefaultConnection", "ConnectionStrings");
            int userOption = Convert.ToInt32(Console.ReadLine());
            switch (userOption)
            {
                case 0: await ProcessJsonFileAsync<Employee>("empData.json", "Employee", connStr);
                    break;
                case 1:
                    await ProcessDataFromJson<Product>("Products.json", "Product");
                    break;
            }

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

                    string saveFolder = Path.Combine(Directory.GetCurrentDirectory(), "SavedFiles");
                    //await SaveInFile.SaveJsonToFileAsync(model, saveFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_output.json");
                    await SaveInFile.SaveExcelToFileAsync(model, saveFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_output.xlsm");

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

            _logger.LogInformation($"{fileName} File processed completed.");

        }
        private async Task ProcessDataFromJson <T> (string fileName, string tableName)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "InputFiles", fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: " + filePath);
            }
            try
            {
                var jsonData = await File.ReadAllTextAsync(filePath);
                Console.WriteLine("here");
               
               await _dataHandlerService.AddDataToDb<T>(JsonSerializer.Deserialize<T>(jsonData), tableName);
                Console.WriteLine("here2");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message} ");
            }
        }
    }
}
