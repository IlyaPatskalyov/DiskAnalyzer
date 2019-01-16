using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using DiskAnalyzer.Model;

namespace DiskAnalyzer.Services
{
    public interface IStatisticsService
    {
        ObservableCollection<StatisticsItem> GetCollection(string name);
        void Cleanup();
        Task CalculateAsync(IFileSystemNode node, CancellationToken token, SynchronizationContext synchronizationContext);
    }
}