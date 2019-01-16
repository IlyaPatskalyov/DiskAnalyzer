using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Autofac.Features.OwnedInstances;
using DiskAnalyzer.Model;
using JetBrains.Annotations;
using Serilog;

namespace DiskAnalyzer.Services
{
    public class FileSystemService : IDisposable, IFileSystemService
    {
        private readonly ILogger logger;
        private readonly FileSystemNode rootNode;
        private readonly ConcurrentDictionary<string, FileSystemWatcher> watchers;

        public FileSystemService(ILogger logger, Func<Owned<FileSystemNode>> nodeFactory)
        {
            this.logger = logger;
            rootNode = nodeFactory().Value;
            watchers = new ConcurrentDictionary<string, FileSystemWatcher>();
        }

        public void Dispose()
        {
            foreach (var w in watchers)
            {
                w.Value.EnableRaisingEvents = false;
            }
        }

        public FileSystemNode Root => rootNode;

        public IFileSystemNode GetDrive(string path)
        {
            return rootNode.GetOrCreateChild(path);
        }

        public void StopWatcher(string path)
        {
            watchers.GetOrAdd(path, CreateWatcher).EnableRaisingEvents = false;
        }

        public void StartWatcher(string path)
        {
            watchers.GetOrAdd(path, CreateWatcher).EnableRaisingEvents = true;
        }


        public void Scan(string path, CancellationToken tsToken)
        {
            rootNode.GetChild(path)?.CleanupNode();
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
                catch (Exception e)
                {
                    logger.Warning(e, "Error access to folder {folder}", r.FullName);
                }
            }
        }

        private FileSystemWatcher CreateWatcher([NotNull] string path)
        {
            var watcher = new FileSystemWatcher
                          {
                              Path = path,
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
            logger.Debug("A new file has been renamed from {oldName} to {name}", e.OldName, e.Name);

            rootNode.GetOrCreateChild(e.OldFullPath).UpdateInfo(FileType.Unknown, newSize: 0, newCreationTime: null);
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
            logger.Debug("A new file has been deleted - {name} ", e.Name);

            rootNode.GetOrCreateChild(e.FullPath).UpdateInfo(FileType.Unknown, newSize: 0, newCreationTime: null);
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            logger.Debug("A new file has been changed - {name}", e.Name);

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
            logger.Debug("A new file has been created - {name}", e.Name);

            var fileInfo = new FileInfo(e.FullPath);
            var dirInfo = new DirectoryInfo(e.FullPath);
            fileInfo.Refresh();
            dirInfo.Refresh();
            if (fileInfo.Exists)
                rootNode.GetOrCreateChild(e.FullPath).UpdateInfo(FileType.File, fileInfo.Length, fileInfo.CreationTime);
            else if (dirInfo.Exists)
            {
                rootNode.GetOrCreateChild(e.FullPath).UpdateInfo(FileType.Directory, newSize: 0, dirInfo.CreationTime);
                Scan(e.FullPath, new CancellationTokenSource().Token);
            }
        }
    }
}