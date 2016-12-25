using GalaSoft.MvvmLight.Threading;

namespace RavenAdminLogsCollectionTool
{
    public partial class App
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }
    }
}
