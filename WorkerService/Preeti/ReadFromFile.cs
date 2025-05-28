using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;
using WorkerService.Models;

namespace WorkerService.Preeti
{
    public static class ReadFromFile
    {
        public static List<UserProfile> ReadExcelToUserProfiles(string filePath)
        {
            var userProfiles = new List<UserProfile>();

            using var workbook = new XLWorkbook(filePath);
            var mainSheet = workbook.Worksheet("Main");

            var rows = mainSheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var user = new UserProfile
                {
                    UserId = int.Parse(row.Cell(1).GetString()),
                    FullName = row.Cell(2).GetString(),
                    Email = row.Cell(3).GetString()
                };

                userProfiles.Add(user);
            }

            foreach (var sheet in workbook.Worksheets.Where(ws => ws.Name != "Main"))
            {
                if (sheet.Name == "Roles")
                {
                    foreach (var row in sheet.RowsUsed().Skip(1))
                    {
                        int userId = int.Parse(row.Cell(1).GetString());
                        var user = userProfiles.FirstOrDefault(u => u.UserId == userId);
                        if (user != null)
                        {
                            var roles = row.Cell(3).GetString().Split(", ").ToList();
                            user.Roles = roles;
                        }
                    }
                }
                else if (sheet.Name == "Preferences")
                {
                    var headers = sheet.Row(1).Cells().Select(c => c.GetString()).ToList();

                    foreach (var row in sheet.RowsUsed().Skip(1))
                    {
                        int userId = int.Parse(row.Cell(1).GetString());
                        var user = userProfiles.FirstOrDefault(u => u.UserId == userId);
                        if (user != null)
                        {
                            var preferences = new Dictionary<string, string>();
                            for (int i = 3; i <= headers.Count; i++)
                            {
                                var key = headers[i - 1];
                                var value = row.Cell(i).GetString();
                                preferences[key] = value;
                            }
                            user.Preferences = preferences;
                        }
                    }
                }
            }

            return userProfiles;
        }
    }
}
