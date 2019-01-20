using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DiskAnalyzer.Model;
using JetBrains.Annotations;

namespace DiskAnalyzer.Statistics
{
    [UsedImplicitly]
    public class TopFilesBySizeCalculator : IStatisticsCalculator
    {
        public IEnumerable<StatisticsItem> Calculate(IFileSystemNode node, CancellationToken token)
        {
            return node.Search(token)
                       .Where(r => r.FileType == FileType.File)
                       .OrderByDescending(a => a.Size)
                       .Select(r => new StatisticsItem
                                    {
                                        Name = r.GetFullPath(),
                                        Path = r.GetFullPath(),
                                        Size = r.Size,
                                        CountFiles = r.CountFiles,
                                    })
                       .Take(50);
        }
    }
}