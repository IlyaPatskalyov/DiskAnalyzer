using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Serilog;
using Timer = System.Timers.Timer;

namespace DiskAnalyzer.Services
{
    [UsedImplicitly]
    public class DriveService : IDriveService, IDisposable
    {
        private readonly ILogger logger;
        private readonly ConcurrentDictionary<ObservableCollection<DriveInfo>, Timer> timers;

        public DriveService(ILogger rootLogger)
        {
            timers = new ConcurrentDictionary<ObservableCollection<DriveInfo>, Timer>();
            logger = rootLogger.ForContext<DriveService>();
        }

        public void Dispose()
        {
            foreach (var t in timers)
            {
                t.Value.Enabled = false;
            }
        }

        public void StartWatcher(SynchronizationContext synchronizationContext, ObservableCollection<DriveInfo> observable)
        {
            timers.GetOrAdd(observable, k => CreateTimer(synchronizationContext, observable)).Enabled = true;
            synchronizationContext.Send(a => UpdateDrives(observable), null);
        }

        private Timer CreateTimer(SynchronizationContext synchronizationContext, ObservableCollection<DriveInfo> observable)
        {
            var timer = new Timer() {Interval = 5000};
            timer.Elapsed += (v, e) => synchronizationContext.Send(a => UpdateDrives(observable), v);
            return timer;
        }

        public void StopWatcher(ObservableCollection<DriveInfo> observable)
        {
            if (timers.TryRemove(observable, out var timer))
            {
                timer.Enabled = false;
            }
        }

        private void UpdateDrives(ObservableCollection<DriveInfo> oldValues)
        {
            var newValues = DriveInfo.GetDrives().Where(d => d.IsReady).ToArray();
            var newDriveInfos = IndexDrives(newValues);
            var oldDriveInfos = IndexDrives(oldValues);

            var deletedDrives = oldDriveInfos.Keys.Except(newDriveInfos.Keys).ToArray();
            foreach (var name in deletedDrives.OrderByDescending(d => oldDriveInfos[d]))
            {
                oldValues.RemoveAt(oldDriveInfos[name]);
            }

            var addedDrives = newDriveInfos.Keys.Except(oldDriveInfos.Keys).ToArray();
            foreach (var name in addedDrives)
            {
                var index = newDriveInfos[name];
                oldValues.Insert(index, newValues[index]);
            }

            if (deletedDrives.Length > 0 || addedDrives.Length > 0)
            {
                logger.Information("Changes in drives. Added: {added}, Deleted: {deleted}", addedDrives, deletedDrives);
            }
        }

        private static Dictionary<string, int> IndexDrives(IEnumerable<DriveInfo> driveInfos)
            => driveInfos
               .Select((drive, index) => new {Drive = drive.Name, Index = index})
               .ToDictionary(k => k.Drive, v => v.Index);
    }
}