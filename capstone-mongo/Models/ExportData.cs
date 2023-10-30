using System;
using System.Reflection;
using System.Text;

namespace capstone_mongo.Models
{
    public class ExportData
    {
        public static void ExportCsv<T>(List<T> genericList, string fileName)
        {
            var sb = new StringBuilder();
            var finalPath = Path.Combine(
                       Directory.GetCurrentDirectory(), "wwwroot",
                       (fileName + ".csv"));
            var header = "";
            var info = typeof(T).GetProperties();

            // TODO: Not overwriting file or creating a new one
            // FIXME: Appends to existing file
            if (!File.Exists(finalPath))
            {
                var file = File.Create(finalPath);
                file.Close();
                foreach (var prop in typeof(T).GetProperties())
                {
                    header += prop.Name + ", ";
                }
                header = header.Substring(0, header.Length - 2);
                sb.AppendLine(header);
                TextWriter sw = new StreamWriter(finalPath, true);
                sw.Write(sb.ToString());
                sw.Close();
            }

            // TODO: REMOVE ID IN FIRST COLUMN
            foreach (var obj in genericList)
            {
                sb = new StringBuilder();
                var line = "";
                foreach (var prop in info)
                {
                    line += prop.GetValue(obj, null) + ", ";
                }
                line = line.Substring(0, line.Length - 2);
                sb.AppendLine(line);
                TextWriter sw = new StreamWriter(finalPath, true);
                sw.Write(sb.ToString());
                sw.Close();
            }
        }
    }
}

