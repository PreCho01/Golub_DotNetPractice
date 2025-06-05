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
            //await ProcessJsonFileAsync<Employee>("empData.json", "Employee", connStr);
            //await ProcessJsonFileAsync<List<StudentProfile>>("Student.json", "StudentProfile", connStr);
            //await ReadUserProfilesFromExcel("User_output.xlsm");
            await ReadAndSaveFromExcel<UserProfile>("User_output.xlsm", "UserProfile");

        }

        //Preeti
        private async Task ProcessJsonFileAsync<T>(string fileName, string tableName, string connStr)
        {
            string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "InputFiles", fileName);

            if (File.Exists(jsonFilePath))
            {
                try
                {
                    var jsonData = await File.ReadAllTextAsync(jsonFilePath);

                    var model = JsonSerializer.Deserialize<T>(jsonData);
                    var handler = new DataHandler<object>(connStr);
                    string saveFolder = Path.Combine(Directory.GetCurrentDirectory(), "SavedFiles");
                  
                    if (model is IEnumerable<object> list)
                    {
                        foreach (var item in list)
                        {
                            await handler.SaveComplexDataAsync(item, tableName);
                        }
                        //await SaveInFile.SaveJsonToFileAsync(model, saveFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_output.json");
                        await SaveInFile.SaveComplexExcelToFileAsync(model, saveFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_output.xlsm");

                    }
                    else
                    {

                        await handler.SaveComplexDataAsync(model, tableName); 
                        //await SaveInFile.SaveJsonToFileAsync(model, saveFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_output.json");
                        await SaveInFile.SaveComplexExcelToFileAsync(model, saveFolder, $"{Path.GetFileNameWithoutExtension(fileName)}_output.xlsm");
                    }

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

        //Preeti
        private async Task ReadUserProfilesFromExcel(string fileName)
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SavedFiles", fileName);

            if (File.Exists(filePath))
            {
                var users = ReadFromFile.ReadExcelToUserProfiles(filePath);
                var connStr = _config.GetAppSettings("DefaultConnection", "ConnectionStrings");
                await SaveUserProfilesToDatabase(users, "UserProfile", connStr);
            }
            else
            {
                _logger.LogWarning($"Excel file not found at {filePath}");
            }

            await Task.CompletedTask;
        }

        //Preeti
        private async Task SaveUserProfilesToDatabase(List<UserProfile> users, string tableName, string connStr)
        {
            var handler = new DataHandler<object>(connStr);

            foreach (var user in users)
            {
                await handler.SaveComplexDataAsync(user, tableName);
            }

            _logger.LogInformation($"Saved {users.Count} user profiles to table {tableName}.");
        }

        //Generic - Preeti
        private async Task ReadAndSaveFromExcel<T>(string fileName, string tableName) where T : new()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "SavedFiles", fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Excel file not found at {filePath}");
                return;
            }

            var objects = ReadFromFile.ReadExcelToObjects<T>(filePath);

            foreach (var obj in objects)
            {
                _logger.LogInformation($"Object: {JsonSerializer.Serialize(obj)}");
            }

            var connStr = _config.GetAppSettings("DefaultConnection", "ConnectionStrings");
            var handler = new DataHandler<object>(connStr);

            foreach (var obj in objects)
            {
                await handler.SaveComplexDataAsync(obj, tableName);
            }

            _logger.LogInformation($"Saved {objects.Count} records to table {tableName}.");
        }

    }
}

