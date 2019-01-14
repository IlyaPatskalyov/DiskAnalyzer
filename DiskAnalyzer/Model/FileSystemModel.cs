using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DiskAnalyzer.Model
{
    public class FileSystemModel : IDisposable
    {
        private readonly FileSystemWatcher fileSystemWatcher;
        private readonly FileSystemNode rootNode;
        private readonly string rootPath;

        public FileSystemModel(string rootPath, SynchronizationContext synchronizationContext)
        {
            rootNode = new FileSystemNode(synchronizationContext);
            this.rootPath = rootPath;
            fileSystemWatcher = CreateWatcher();
        }

        public FileSystemNode Root => rootNode.GetOrCreateChild(rootPath);

        public void Dispose()
        {
            StopWatcher();
        }

        public void StartWatcher()
        {
            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private FileSystemWatcher CreateWatcher()
        {
            var watcher = new FileSystemWatcher
                          {
                              Path = rootPath,
                              IncludeSubdirectories = true
                          };
            watcher.Created += FileSystemWatcher_Created;
            watcher.Changed += FileSystemWatcher_Changed;
            watcher.Deleted += FileSystemWatcher_Deleted;
            watcher.Renamed += FileSystemWatcher_Renamed;
            return watcher;
        }

        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            rootNode.GetOrCreateChild(e.OldFullPath).UpdateInfo(FileType.Unknown, 0, newCreationTime: null);
            var fileInfo = new FileInfo(e.FullPath);
            var dirInfo = new DirectoryInfo(e.FullPath);
            fileInfo.Refresh();
            dirInfo.Refresh();

            if (fileInfo.Exists)
            {
                rootNode.GetOrCreateChild(e.FullPath).UpdateInfo(FileType.File, fileInfo.Length, fileInfo.CreationTime);
            }
            else if (dirInfo.Exists)
            {
                var node = rootNode.RemoveNode(e.OldFullPath);
                if (node != null)
                    rootNode.InsertNode(e.FullPath, node);
                else
                    throw new Exception("Race condition");
            }
        }

        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            rootNode.GetOrCreateChild(e.FullPath).UpdateInfo(FileType.Unknown, 0, newCreationTime: null);
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var fileInfo = new FileInfo(e.FullPath);
            var dirInfo = new DirectoryInfo(e.FullPath);
            fileInfo.Refresh();
            dirInfo.Refresh();
            if (fileInfo.Exists)
                rootNode.GetOrCreateChild(e.FullPath).UpdateInfo(FileType.File, fileInfo.Length, fileInfo.CreationTime);
            else if (dirInfo.Exists)
                rootNode.GetOrCreateChild(e.FullPath).UpdateInfo(FileType.Directory, newSize: null, dirInfo.CreationTime);
        }

        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            var fileInfo = new FileInfo(e.FullPath);
            var dirInfo = new DirectoryInfo(e.FullPath);
            fileInfo.Refresh();
            dirInfo.Refresh();
            if (fileInfo.Exists)
                rootNode.GetOrCreateChild(e.FullPath).UpdateInfo(FileType.File, fileInfo.Length, fileInfo.CreationTime);
            else if (dirInfo.Exists)
                rootNode.GetOrCreateChild(e.FullPath).UpdateInfo(FileType.Directory, 0, dirInfo.CreationTime);
        }

        public void Refresh(CancellationToken tsToken)
        {
            Initialize(rootPath, tsToken);
        }

        private void Initialize(string path, CancellationToken tsToken)
        {
            rootNode.RemoveNode(path);
            var queue = new Queue<DirectoryInfo>();
            queue.Enqueue(new DirectoryInfo(path));

            while (queue.Count > 0)
            {
                if (tsToken.IsCancellationRequested)
                    return;

                var r = queue.Dequeue();
                try
                {
                    foreach (var d in r.GetDirectories())
                    {
                        if (tsToken.IsCancellationRequested)
                            return;
                        rootNode.GetOrCreateChild(d.FullName).UpdateInfo(FileType.Directory, 0, d.CreationTime);
                        queue.Enqueue(d);
                    }

                    foreach (var f in r.GetFiles())
                    {
                        if (tsToken.IsCancellationRequested)
                            return;
                        rootNode.GetOrCreateChild(f.FullName).UpdateInfo(FileType.File, f.Length, f.CreationTime);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        public void StopWatcher()
        {
            fileSystemWatcher.EnableRaisingEvents = false;
        }
    }
}