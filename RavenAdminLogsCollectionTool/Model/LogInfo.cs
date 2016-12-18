using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RavenAdminLogsCollectionTool.Model
{
    public class LogInfo
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel Level { get; set; }
        public string Database { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Message { get; set; }
        public string LoggerName { get; set; }
        public string Exception { get; set; }
        public string StackTrace { get; set; }

        public override string ToString()
        {
            string str = $"{TimeStamp:o};{Level.ToString().ToUpper()};{Database};{LoggerName};{Message}{Exception ?? String.Empty}\n";
            if (!String.IsNullOrEmpty(StackTrace))
            {
                str += StackTrace + "\n\n";
            }
            return str;
        }
    }
}
