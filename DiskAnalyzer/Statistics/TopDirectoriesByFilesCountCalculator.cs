using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DiskAnalyzer.Model;
using JetBrains.Annotations;

namespace DiskAnalyzer.Statistics
{
    [UsedImplicitly]
    public class TopDirectoriesByFilesCountCalculator : IStatisticsCalculator
    {
        public IEnumerable<StatisticsItem> Calculate(IFileSystemNode node, CancellationToken token)
        {
            return node.Search(token)
                       .Where(r => r.FileType == FileType.Directory &&
                                   0.95 * r.CountFiles > r
                                                         .Children.Where(z => z.FileType == FileType.Directory)
                                                         .Select(z => z.CountFiles)
                                                         .DefaultIfEmpty().Max())
                       .OrderByDescending(a => a.CountFiles)
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