using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using JetBrains.Annotations;

namespace DiskAnalyzer.Model
{
    public interface IFileSystemNode : INotifyCollectionChanged, INotifyPropertyChanged
    {
        string Name { get; }
        long Size { get; }
        DateTime? CreationTime { get; }
        int CountFiles { get; }
        int CountDirectories { get; }
        IFileSystemNode Parent { get; }
        FileType FileType { get; }
        IEnumerable<IFileSystemNode> Children { get; }

        [CanBeNull]
        IFileSystemNode GetChild(string path);

        IEnumerable<IFileSystemNode> Search(CancellationToken token);
        string GetFullPath();
    }
}