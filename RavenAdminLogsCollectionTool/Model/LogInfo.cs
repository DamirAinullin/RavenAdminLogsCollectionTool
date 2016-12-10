﻿using System;

namespace RavenAdminLogsCollectionTool.Model
{
    public class LogInfo
    {
        public LogLevel LogLevel { get; set; }
        public string Database { get; set; }
        public string TimeStamp { get; set; }
        public string Message { get; set; }
        public string LoggerName { get; set; }
        public string Exception { get; set; }
        public string StackTrace { get; set; }

        public override string ToString()
        {
            string str = $"{TimeStamp};{LogLevel.ToString().ToUpper()};{Database};{LoggerName};{Message}{Exception ?? String.Empty}\n";
            if (!String.IsNullOrEmpty(StackTrace))
            {
                str += StackTrace + "\n\n";
            }
            return str;
        }
    }
}
