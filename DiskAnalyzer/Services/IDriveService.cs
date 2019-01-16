using System.Collections.ObjectModel;
using System.IO;
using System.Threading;

namespace DiskAnalyzer.Services
{
    public interface IDriveService
    {
        void StartWatcher(SynchronizationContext synchronizationContext, ObservableCollection<DriveInfo> observable);
        void StopWatcher(ObservableCollection<DriveInfo> observable);
    }
}