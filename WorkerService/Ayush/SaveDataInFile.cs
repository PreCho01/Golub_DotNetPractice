using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using OfficeOpenXml;
using WorkerService.Models;

namespace WorkerService.Ayush
{
    public static class SaveDataInFile
    {
        
        // Async method to load users from JSON
        private static async Task<List<User>> LoadUsersFromJsonAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("JSON file not found.", filePath);

            string json = await File.ReadAllTextAsync(filePath);
            return JsonConvert.DeserializeObject<List<User>>(json);
        }

        // Async method to save to SQL
        public static async Task SaveToSqlDatabaseAsync(string jsonFilePath, string connectionString)
        {
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

        // Async method to write to JSON file
        public static async Task SaveToJsonFileAsync(string jsonFilePath, string outputFilePath)
        {
            var users = await LoadUsersFromJsonAsync(jsonFilePath);
            string outputJson = JsonConvert.SerializeObject(users, Formatting.Indented);
            await File.WriteAllTextAsync(outputFilePath, outputJson);
        }

        // Excel file write (sync, no async APIs in EPPlus)
        public static void SaveToExcelFile(string jsonFilePath, string excelFilePath)
        {
            var users = LoadUsersFromJsonAsync(jsonFilePath).Result;

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Users");

                worksheet.Cells[1, 1].Value = "Id";
                worksheet.Cells[1, 2].Value = "Name";
                worksheet.Cells[1, 3].Value = "Age";

                for (int i = 0; i < users.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = users[i].Id;
                    worksheet.Cells[i + 2, 2].Value = users[i].Name;
                    worksheet.Cells[i + 2, 3].Value = users[i].Age;
                }

                File.WriteAllBytes(excelFilePath, package.GetAsByteArray());
            }
        }
    }
}
