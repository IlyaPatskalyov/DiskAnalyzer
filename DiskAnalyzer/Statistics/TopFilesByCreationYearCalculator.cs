using System.Collections.Generic;
using System.Linq;
using DiskAnalyzer.Model;
using JetBrains.Annotations;

namespace DiskAnalyzer.Statistics
{
    [UsedImplicitly]
    public class TopFilesByCreationYearCalculator : IStatisticsCalculator
    {
        public IEnumerable<StatisticsItem> Calculate(IFileSystemNode node)
        {
            return node.Search()
                       .Where(r => r.FileType == FileType.File && r.CreationTime.HasValue)
                       .GroupBy(r => r.CreationTime.Value.Year)
                       .Select(r => new StatisticsItem()
                                    {
                                        Name = r.Key.ToString(),
                                        Size = r.Sum(n => n.Size),
                                        CountFiles = r.Count(),
                                    })
                       .OrderByDescending(a => a.Size)
                       .ToList();
        }
    }
}