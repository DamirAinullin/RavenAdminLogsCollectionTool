namespace RavenAdminLogsCollectionTool.Services
{
    public interface IConfigurationService
    {
        string GetDatabaseUrl();
        void SetDatabaseUrl(string databaseUrl);
    }
}
