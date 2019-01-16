using System.Threading;
using DiskAnalyzer.Model;
using JetBrains.Annotations;

namespace DiskAnalyzer.Services
{
    public interface IFileSystemService
    {
        FileSystemNode Root { get; }
        IFileSystemNode GetDrive([NotNull] string path);
        void StopWatcher([NotNull] string path);
        void StartWatcher([NotNull] string path);
        void Scan([NotNull] string path, CancellationToken tsToken);
    }
}