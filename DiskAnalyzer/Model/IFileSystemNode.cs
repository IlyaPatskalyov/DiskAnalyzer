using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

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

        IEnumerable<IFileSystemNode> Search();
        string GetFullPath();
    }
}