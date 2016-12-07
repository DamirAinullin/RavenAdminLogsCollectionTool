using System.Configuration;

namespace RavenAdminLogsCollectionTool.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public string GetValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public void SetValue(string key, string value)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["DatabaseUrl"].Value = value;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
