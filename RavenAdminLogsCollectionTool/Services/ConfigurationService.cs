using System.Configuration;

namespace RavenAdminLogsCollectionTool.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public string GetDatabaseUrl()
        {
            return ConfigurationManager.AppSettings["DatabaseUrl"];
        }

        public void SetDatabaseUrl(string databaseUrl)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["DatabaseUrl"].Value = databaseUrl;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
