using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RavenAdminLogsCollectionTool.Model;

namespace RavenAdminLogsCollectionTool.Services
{
    public class FileSystemService : IFileSystemService
    {
        private string _path;
        private readonly object _syncFileObject = new object();

        public string SaveLogFile(string content)
        {
            string pathUser = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string path = Path.Combine(pathUser, "Downloads", "logs.json");
            if (File.Exists(path))
            {
                path = NextAvailableFilename(path);
            }
            lock (_syncFileObject)
            {
                File.WriteAllText(path, content);
            }
            _path = path;
            return path;
        }

        public bool LogFileExists()
        {
            if (_path != null && File.Exists(_path))
            {
                return true;
            }
            string pathUser = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _path = Path.Combine(pathUser, "Downloads", "logs.json");
            return File.Exists(_path);
        }

        public async Task<List<LogInfo>> LoadLogsFromFileAsync()
        {
            using (var reader = File.OpenText(_path))
            {
                var fileText = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<List<LogInfo>>(fileText);
            }
        }

        public void SaveLogMessageToFile(string logMessageText)
        {
            if (_path == null || !File.Exists(_path))
            {
                string pathUser = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                _path = Path.Combine(pathUser, "Downloads", "logs.json");
            }
            lock (_syncFileObject)
            {
                File.AppendAllText(_path, logMessageText);
            }
        }

        private string NextAvailableFilename(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }
            if (Path.HasExtension(path))
            {
                return GetNextFilename(path.Insert(path.LastIndexOf(Path.GetExtension(path), StringComparison.Ordinal), " ({0})"));
            }
            return GetNextFilename(path + " ({0})");
        }

        private string GetNextFilename(string pattern)
        {
            string tmp = string.Format(pattern, 1);

            if (!File.Exists(tmp))
            {
                return tmp;
            }
            int min = 1;
            int max = 2;

            while (File.Exists(string.Format(pattern, max)))
            {
                min = max;
                max *= 2;
            }

            while (max != min + 1)
            {
                int pivot = (max + min) / 2;
                if (File.Exists(string.Format(pattern, pivot)))
                {
                    min = pivot;
                }
                else
                {
                    max = pivot;
                }
            }

            return string.Format(pattern, max);
        }
    }
}
