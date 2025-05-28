using System.Collections;
using ClosedXML.Excel;
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

        public static async Task SaveExcelToFileAsync<T>(T data, string folderPath, string fileName)
        {             
            if (!Directory.Exists(folderPath)) 
            {              
                Directory.CreateDirectory(folderPath);        
            }
            string fullPath = Path.Combine(folderPath, fileName);

            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Data");

                var props = typeof(T).GetProperties();
                for (int i = 0; i < props.Length; i++)
                {
                    ws.Cell(1, i + 1).Value = props[i].Name;                           
                    ws.Cell(2, i + 1).Value = props[i].GetValue(data)?.ToString();     
                }

                wb.SaveAs(fullPath); 
            });
        }

        public static async Task SaveComplexExcelToFileAsync<T>(T data, string folderPath, string fileName)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            //string fileBaseName = Path.GetFileNameWithoutExtension(fileName);
            //string extension = Path.GetExtension(fileName);
            //string datePart = DateTime.Now.ToString("yyyyMMdd");
            //string baseFileNameWithDate = $"{fileBaseName}_{datePart}";
            //string fullPath = Path.Combine(folderPath, baseFileNameWithDate + extension);

            //// Check for existing files and add versioning
            //int version = 1;
            //while (File.Exists(fullPath))
            //{
            //    string versionSuffix = $"_V{version}";
            //    fullPath = Path.Combine(folderPath, baseFileNameWithDate + versionSuffix + extension);
            //    version++;
            //}

            string fullPath = Path.Combine(folderPath, fileName);

            await Task.Run(() =>
            {
                using var wb = new XLWorkbook();

                var isList = data is IEnumerable<object>;
                var dataList = isList ? ((IEnumerable<object>)data).ToList() : new List<object> { data };

                var firstItem = dataList.FirstOrDefault();
                if (firstItem == null)
                    return;

                var simpleProps = firstItem.GetType().GetProperties()
                    .Where(p => !typeof(IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string))
                    .ToList();

                var idProps = simpleProps.Take(2).ToList(); 

                // MAIN SHEET
                var mainSheet = wb.Worksheets.Add("Main");

                for (int i = 0; i < simpleProps.Count; i++)
                    mainSheet.Cell(1, i + 1).Value = simpleProps[i].Name;

                int mainRow = 2;
                foreach (var item in dataList)
                {
                    for (int i = 0; i < simpleProps.Count; i++)
                    {
                        var value = simpleProps[i].GetValue(item, null);
                        mainSheet.Cell(mainRow, i + 1).Value = value?.ToString();
                    }
                    mainRow++;
                }

                ApplySheetStyle(mainSheet);

                // COMPLEX PROPERTIES
                var complexProps = firstItem.GetType().GetProperties()
                    .Where(p => typeof(IEnumerable).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))
                    .ToList();

                foreach (var prop in complexProps)
                {
                    var sheet = wb.Worksheets.Add(prop.Name);
                    var isDictionary = prop.PropertyType.IsGenericType &&
                                       prop.PropertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>);

                    // Header
                    sheet.Cell(1, 1).Value = idProps[0].Name;
                    sheet.Cell(1, 2).Value = idProps[1].Name;

                    if (isDictionary)
                    {
                        var allKeys = new HashSet<string>();

                        foreach (var item in dataList)
                        {
                            var dict = prop.GetValue(item, null) as IDictionary;
                            if (dict != null)
                            {
                                foreach (DictionaryEntry entry in dict)
                                    allKeys.Add(entry.Key.ToString());
                            }
                        }

                        var keys = allKeys.OrderBy(k => k).ToList();
                        for (int i = 0; i < keys.Count; i++)
                            sheet.Cell(1, i + 3).Value = keys[i];

                        int row = 2;
                        foreach (var item in dataList)
                        {
                            sheet.Cell(row, 1).Value = idProps[0].GetValue(item, null)?.ToString();
                            sheet.Cell(row, 2).Value = idProps[1].GetValue(item, null)?.ToString();

                            var dict = prop.GetValue(item, null) as IDictionary;
                            for (int i = 0; i < keys.Count; i++)
                            {
                                var val = dict?[keys[i]];
                                sheet.Cell(row, i + 3).Value = val?.ToString();
                            }

                            row++;
                        }
                    }
                    else
                    {
                        // List<string> or List<T>
                        sheet.Cell(1, 3).Value = prop.Name;

                        int row = 2;
                        foreach (var item in dataList)
                        {
                            sheet.Cell(row, 1).Value = idProps[0].GetValue(item, null)?.ToString();
                            sheet.Cell(row, 2).Value = idProps[1].GetValue(item, null)?.ToString();

                            var list = prop.GetValue(item, null) as IEnumerable;
                            if (list != null)
                            {
                                var joined = string.Join(", ", list.Cast<object>());
                                sheet.Cell(row, 3).Value = joined;
                            }

                            row++;
                        }
                    }

                    ApplySheetStyle(sheet);
                }

                wb.SaveAs(fullPath);
            });
        }

        private static void ApplySheetStyle(IXLWorksheet sheet)
        {
            var usedRange = sheet.RangeUsed();
            if (usedRange == null) return;

            var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            if (lastCol > 0)
            {
                var headerRow = sheet.Row(1);
                for (int col = 1; col <= lastCol; col++)
                {
                    var cell = headerRow.Cell(col);
                    if (!string.IsNullOrWhiteSpace(cell.GetString()))
                    {
                        cell.Style.Font.Bold = true;
                        cell.Style.Fill.BackgroundColor = XLColor.Yellow;
                    }
                }
            }

            usedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            usedRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            sheet.Columns().AdjustToContents();
        }

    }
}
