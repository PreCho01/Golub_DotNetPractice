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
}
}
