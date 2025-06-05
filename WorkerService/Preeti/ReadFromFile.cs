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

        //generic
        public static List<T> ReadExcelToObjects<T>(string filePath) where T : new()
        {
            var result = new List<T>();
            using var workbook = new XLWorkbook(filePath);

            var type = typeof(T);
            var props = type.GetProperties();

            //Reading Main sheet for simple properties
            var mainSheet = workbook.Worksheet("Main");
            var headers = mainSheet.Row(1).Cells().Select(c => c.GetString()).ToList();

            foreach (var row in mainSheet.RowsUsed().Skip(1))
            {
                var obj = new T();

                for (int i = 0; i < headers.Count; i++)
                {
                    var prop = props.FirstOrDefault(p => p.Name == headers[i]);
                    if (prop != null && !typeof(IEnumerable).IsAssignableFrom(prop.PropertyType) || prop.PropertyType == typeof(string))
                    {
                        var cellValue = row.Cell(i + 1).GetString();
                        object? convertedValue = Convert.ChangeType(cellValue, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                        prop.SetValue(obj, convertedValue);
                    }
                }

                result.Add(obj);
            }

            //Reading complex properties from other sheets
            var idProps = props.Where(p => !typeof(IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string)).Take(2).ToList();

            foreach (var prop in props.Where(p => typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string)))
            {
                var sheet = workbook.Worksheet(prop.Name);
                if (sheet == null) continue;

                var sheetHeaders = sheet.Row(1).Cells().Select(c => c.GetString()).ToList();

                foreach (var row in sheet.RowsUsed().Skip(1))
                {
                    var id1 = row.Cell(1).GetString();
                    var id2 = row.Cell(2).GetString();

                    var obj = result.FirstOrDefault(o =>
                        idProps.Count > 0 && idProps[0].GetValue(o)?.ToString() == id1 &&
                        (idProps.Count < 2 || idProps[1].GetValue(o)?.ToString() == id2));

                    if (obj == null) continue;

                    if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        var dict = (IDictionary)Activator.CreateInstance(prop.PropertyType)!;
                        for (int i = 3; i <= sheetHeaders.Count; i++)
                        {
                            var key = sheetHeaders[i - 1];
                            var val = row.Cell(i).GetString();
                            dict.Add(key, val);
                        }
                        prop.SetValue(obj, dict);
                    }
                    else
                    {
                        var list = row.Cell(3).GetString().Split(", ").ToList();
                        var listType = typeof(List<>).MakeGenericType(prop.PropertyType.GetGenericArguments()[0]);
                        var listInstance = (IList)Activator.CreateInstance(listType)!;
                        foreach (var item in list)
                        {
                            listInstance.Add(Convert.ChangeType(item, prop.PropertyType.GetGenericArguments()[0]));
                        }
                        prop.SetValue(obj, listInstance);
                    }
                }
            }

            return result;
        }
    }
}
