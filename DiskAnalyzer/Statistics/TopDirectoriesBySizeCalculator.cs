using System.Collections.Generic;
using System.Linq;
using DiskAnalyzer.Model;
using JetBrains.Annotations;

namespace DiskAnalyzer.Statistics
{
    [UsedImplicitly]
    public class TopDirectoriesBySizeCalculator : IStatisticsCalculator
    {
        public IEnumerable<StatisticsItem> Calculate(IFileSystemNode node)
        {
            return node.Search()
                       .Where(r => r.FileType == FileType.Directory &&
                                   0.95 * r.Size > r
                                                   .Children.Where(z => z.FileType == FileType.Directory)
                                                   .Select(z => z.Size)
                                                   .DefaultIfEmpty().Max())
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