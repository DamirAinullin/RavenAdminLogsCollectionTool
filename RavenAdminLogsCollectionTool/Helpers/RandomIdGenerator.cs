using System;

namespace RavenAdminLogsCollectionTool.Helpers
{
    public static class RandomIdGenerator
    {
        public static string GenerateId(int idLength = 5)
        {
            var text = String.Empty;
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            for (var i = 0; i < idLength; i++)
            {
                text += chars[(int) Math.Floor(random.NextDouble() * chars.Length)];
            }
            return text;
        }
    }
}
