using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using DiskAnalyzer.Core;
using JetBrains.Annotations;

namespace DiskAnalyzer.Model
{
    public class FileSystemNode : IEnumerable<FileSystemNode>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly SynchronizationContext synchronizationContext;

        private ConcurrentDictionary<string, FileSystemNode> children;
        private int countDirectories;
        private int countFiles;
        private DateTime? creationTime;
        private FileType fileType;
        private string name;
        private FileSystemNode parent;
        private long size;

        public FileSystemNode(SynchronizationContext synchronizationContext)
        {
            this.synchronizationContext = synchronizationContext;
        }

        [CanBeNull] public string Name => name;

        public long Size => size;
        public DateTime? CreationTime => creationTime;

        public int CountFiles => countFiles;

        public int CountDirectories => countDirectories;

        public FileSystemNode Parent => parent;

        public FileType FileType => fileType;

        [NotNull]
        public IEnumerable<FileSystemNode> Children => children?.Values
                                                               .Where(n => n.FileType == FileType.Directory)
                                                               .Concat(children.Values.Where(n => n.FileType == FileType.File))
                                                       ?? Enumerable.Empty<FileSystemNode>();

        public IEnumerator<FileSystemNode> GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public IEnumerable<FileSystemNode> Search()
        {
            var queue = new Queue<FileSystemNode>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                var r = queue.Dequeue();
                yield return r;

                foreach (var d in r.Children)
                {
                    queue.Enqueue(d);
                }
            }
        }

        [NotNull]
        public string GetFullPath()
        {
            var sb = new StringBuilder();
            var t = this;
            while (t != null)
            {
                sb.Insert(0, t.Name);
                if (t.parent != null && t.Name?.EndsWith(":") != true)
                    sb.Insert(0, Path.DirectorySeparatorChar);

                t = t.parent;
            }

            return sb.ToString();
        }

        internal void InsertNode([NotNull] string path, [NotNull] FileSystemNode node)
        {
            var parts = IoHelpers.SplitPath(path);
            if (parts.Length != 2)
                return;
            var parentNode = GetOrCreateChild(parts[0]);
            node.name = parts[1];
            node.parent = parentNode;
            parentNode.children.TryAdd(parts[1], node);

            var t = parentNode;
            while (t != null)
            {
                t.UpdateCounters(node.Size, node.CountDirectories, CountFiles);
                t = t.parent;
            }
        }

        [CanBeNull]
        internal FileSystemNode RemoveNode([NotNull] string path)
        {
            var parts = IoHelpers.SplitPath(path);
            if (children != null && children.TryGetValue(parts[0], out var child))
            {
                if (parts.Length == 1)
                {
                    children.TryRemove(parts[0], out child);
                    child.parent = null;
                    return child;
                }

                var deletedNode = child.RemoveNode(parts[1]);
                if (deletedNode != null)
                {
                    UpdateCounters(-deletedNode.Size, -deletedNode.CountDirectories, -deletedNode.CountFiles);
                    return deletedNode;
                }
            }

            return null;
        }

        [CanBeNull]
        public FileSystemNode GetChild([NotNull] string path)
        {
            var parts = IoHelpers.SplitPath(path);
            if (children != null && children.TryGetValue(parts[0], out var child))
            {
                if (parts.Length == 1) return child;

                return child.GetChild(parts[1]);
            }

            return null;
        }

        [NotNull]
        internal FileSystemNode GetOrCreateChild([NotNull] string path)
        {
            var parts = IoHelpers.SplitPath(path);
            children = children ?? new ConcurrentDictionary<string, FileSystemNode>();
            var child = children.GetOrAdd(parts[0], v => new FileSystemNode(synchronizationContext)
                                                         {
                                                             name = v,
                                                             parent = this
                                                         });

            if (parts.Length == 1) return child;

            return child.GetOrCreateChild(parts[1]);
        }

        internal void UpdateInfo(FileType newFileType, long? newSize, DateTime? newCreationTime)
        {
            var oldSize = size;
            var oldFileType = fileType;
            var oldCreationTime = creationTime;
            if (newSize.HasValue && oldSize != newSize || oldFileType != newFileType)
            {
                var deltaSize = newSize - oldSize ?? 0;
                var deltaFiles = oldFileType == FileType.File && newFileType != FileType.File ? -1 :
                                 oldFileType != FileType.File && newFileType == FileType.File ? 1 : 0;
                var deltaDirectories = oldFileType == FileType.Directory && newFileType != FileType.Directory ? -1 :
                                       oldFileType != FileType.Directory && newFileType == FileType.Directory ? 1 : 0;

                fileType = newFileType;

                if (parent != null && deltaFiles + deltaDirectories > 0)
                    parent.OnCollectionChanged(NotifyCollectionChangedAction.Add, this);

                if (parent != null && deltaFiles + deltaDirectories < 0)
                    parent.OnCollectionChanged(NotifyCollectionChangedAction.Remove, this);

                var t = this;
                while (t != null)
                {
                    t.UpdateCounters(deltaSize, deltaDirectories, deltaFiles);
                    t = t.parent;
                }
            }

            if (oldCreationTime != newCreationTime)
            {
                creationTime = newCreationTime;
                OnPropertyChanged(nameof(CreationTime));
            }
        }

        private void UpdateCounters(long deltaSize, int deltaDirectories, int deltaFiles)
        {
            if (deltaSize != 0)
            {
                size += deltaSize;
                OnPropertyChanged(nameof(Size));
            }

            if (deltaDirectories != 0)
            {
                countDirectories += deltaDirectories;
                OnPropertyChanged(nameof(CountDirectories));
            }

            if (deltaFiles != 0)
            {
                countFiles += deltaFiles;
                OnPropertyChanged(nameof(CountFiles));
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged == null)
                return;

            var args = new PropertyChangedEventArgs(propertyName);
            if (SynchronizationContext.Current == synchronizationContext)
                PropertyChanged.Invoke(this, args);
            else
                synchronizationContext.Send(a => PropertyChanged.Invoke(this, (PropertyChangedEventArgs) a), args);
        }


        protected virtual void OnCollectionChanged(NotifyCollectionChangedAction action, FileSystemNode node = null)
        {
            if (CollectionChanged == null)
                return;

            var args = new NotifyCollectionChangedEventArgs(action, node);
            if (SynchronizationContext.Current == synchronizationContext)
                CollectionChanged.Invoke(this, args);
            else
                synchronizationContext.Send(a => CollectionChanged.Invoke(this, (NotifyCollectionChangedEventArgs) a), args);
        }
    }
}