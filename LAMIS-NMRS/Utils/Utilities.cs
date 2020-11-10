using LAMIS_NMRS.Models;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LAMIS_NMRS.Utils
{
    public class Utilities
    {
        public static string GetAppConfigItem(string itemKey)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            if (configuration.GetSection(itemKey) != null)
            {
                return configuration.GetSection(itemKey).Value;
            }
            return null;
        }
        public static string GetConnectionString(string itemKey)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            string connectionString = builder.Build().GetConnectionString(itemKey);
            return connectionString;
        }

        public static string ScrambleCharacters(string str)
        {
            if (str == null) return "";
            str = str.Trim().ToLower();
            str = str.Replace("a", "^");
            str = str.Replace("b", "~");
            str = str.Replace("c", "`");
            str = str.Replace("e", "*");
            str = str.Replace("f", "$");
            str = str.Replace("g", "#");
            str = str.Replace("h", "@");
            str = str.Replace("i", "!");
            str = str.Replace("j", "%");
            str = str.Replace("k", "|");
            str = str.Replace("n", "}");
            str = str.Replace("o", "{");
            str = str.Replace("'", "");
            return str;
        }

        public static string UnscrambleCharacters(string str)
        {
            if (str == null) return "";
            str = str.Replace("^", "a");
            str = str.Replace("~", "b");
            str = str.Replace("`", "c");
            str = str.Replace("*", "e");
            str = str.Replace("$", "f");
            str = str.Replace("#", "g");
            str = str.Replace("@", "h");
            str = str.Replace("!", "i");
            str = str.Replace("%", "j");
            str = str.Replace("|", "k");
            str = str.Replace("}", "n");
            str = str.Replace("{", "o");
            return UppercaseFirst(str);
        }

        public static string ScrambleNumbers(string str)
        {
            if (str == null) return "";
            str = str.Replace("1", "^");
            str = str.Replace("2", "~");
            str = str.Replace("3", "`");
            str = str.Replace("5", "*");
            str = str.Replace("6", "$");
            str = str.Replace("7", "#");
            str = str.Replace("8", "@");
            str = str.Replace("9", "!");
            return str;
        }

        public static string UnscrambleNumbers(string str)
        {
            if (str == null) return "";
            str = str.Replace("^", "1");
            str = str.Replace("~", "2");
            str = str.Replace("`", "3");
            str = str.Replace("*", "5");
            str = str.Replace("$", "6");
            str = str.Replace("#", "7");
            str = str.Replace("@", "8");
            str = str.Replace("!", "9");
            return str;
        }

        static string UppercaseFirst(string s)
        {
            // Check for empty string.
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            TextInfo cultInfo = new CultureInfo("en-US", false).TextInfo;
            return cultInfo.ToTitleCase(s);
        }

        public List<NmrsConcept> GetConcepts()
        {
            var concepts = new List<NmrsConcept>();
            try
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), @"Templates", @"nmrs_concepts.xlsx");
                FileInfo fileInfo = new FileInfo(path);
                using (var package = new ExcelPackage(fileInfo))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    // get number of rows and columns in the sheets
                    int rows = worksheet.Dimension.Rows;
                    int columns = worksheet.Dimension.Columns;

                    // loop through the worksheet rows and columns
                    for (int i = 2; i <= rows; i++)
                    {
                        var conceptId = worksheet.Cells["A" + i].Value;
                        var uuid = worksheet.Cells["B" + i].Value;

                        concepts.Add(new NmrsConcept
                        {
                            ConceptId = conceptId.ToString(),
                            UuId = uuid.ToString()
                        });
                    }

                }
                return concepts;
            }
            catch (Exception ex)
            {
                var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine(message);
                return new List<NmrsConcept>();
            }
        }
    }   
}
