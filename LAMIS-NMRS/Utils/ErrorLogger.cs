using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Common
{
    public class ErrorLogger
    {
        public static void LogInfo(object message, string DataSet)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(DataSet + ":\n " + message);
                WriteToFile(sb.ToString(), DataSet);
            }
            catch (Exception ex) { Console.WriteLine(ex); }
        }


        public static string LogError(Exception ex, string DataSet)
        {
            string message = "";
            try
            {
                message = GetExceptionMessages(ex);
                WriteToFile(message, DataSet);
            }
            catch { }
            return message;
        }

        static string GetExceptionMessages(Exception ex)
        {
            string ret = string.Empty;
            if (ex != null)
            {
                ret = ex.Message;
                if (ex.InnerException != null)
                    ret = ret + "\n" + GetExceptionMessages(ex.InnerException);
            }
            return ret;
        }

        static void WriteToFile(string msg, string dataSet)
        {
            if (!Directory.Exists("Errors"))
                Directory.CreateDirectory("Errors");

            string filename = $"Errors\\{dataSet}.txt";

            using (StreamWriter str = new System.IO.StreamWriter(filename, true))
            {
                str.WriteLineAsync(msg);
            }
        }
    }
}
