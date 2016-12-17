using System;
using System.IO;

namespace RavenAdminLogsCollectionTool.Services
{
    public class FileSystemService : IFileSystemService
    {
        public string SaveLogFile(string content)
        {
            string pathUser = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string path = Path.Combine(pathUser, "Downloads", "logs.json");
            if (File.Exists(path))
            {
                path = NextAvailableFilename(path);
            }
            File.WriteAllText(path, content);
            return path;
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
