using Newtonsoft.Json;

namespace WorkerService.Preeti
{
    public static class SaveInFile
    {
        public static async Task SaveJsonToFileAsync<T>(T data, string folderPath, string fileName)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            string fullPath = Path.Combine(folderPath, fileName);

            await File.WriteAllTextAsync(fullPath, json);
        }
    }
}
