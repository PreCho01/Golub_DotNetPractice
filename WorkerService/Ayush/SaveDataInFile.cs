using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkerService.Models;

namespace WorkerService.Ayush
{
    public static class SaveDataInFile
    {
        
        // Async method to load users from JSON
        private static async Task<List<Users>> LoadUsersFromJsonAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("JSON file not found.", filePath);

            string json = await File.ReadAllTextAsync(filePath);
            return JsonConvert.DeserializeObject<List<Users>>(json);
        }

        public static async Task SaveToSqlDatabaseAsync(string fileName, string connectionString)
        {
            string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "InputFiles", fileName);
            var users = await LoadUsersFromJsonAsync(jsonFilePath);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                foreach (var user in users)
                {
                    using (SqlCommand cmd = new SqlCommand("INSERT INTO Users (Id, Name, Age) VALUES (@Id, @Name, @Age)", conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", user.Id);
                        cmd.Parameters.AddWithValue("@Name", user.Name);
                        cmd.Parameters.AddWithValue("@Age", user.Age);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        public static async Task SaveToJsonFileAsync(string fileName, string outputFilePath)
        {
            string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "InputFiles", fileName);
            var users = await LoadUsersFromJsonAsync(jsonFilePath);
            string outputJson = JsonConvert.SerializeObject(users, Formatting.Indented);
            await File.WriteAllTextAsync(outputFilePath, outputJson);
        }

        public static async Task SaveToCsvFile(string fileName, string csvFilePath)
        {
            string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "InputFiles", fileName);
            var users = await LoadUsersFromJsonAsync(jsonFilePath);

            using (var writer = new StreamWriter(csvFilePath))
            {
                // Write header
                writer.WriteLine("Id,Name,Age");

                // Write user data
                foreach (var user in users)
                {
                    writer.WriteLine($"{user.Id},{EscapeCsv(user.Name)},{user.Age}");
                }
            }
        }

        // Helper to escape CSV values if needed (e.g. names with commas)
        private static string EscapeCsv(string value)
        {
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }
            return value;
        }
    }
}
