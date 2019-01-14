using System.Collections.Generic;
using System.Linq;
using DiskAnalyzer.Model;

namespace DiskAnalyzer.Statistics
{
    public class TopFilesByCreationYearCalculator : IStatisticsCalculator
    {
        public IEnumerable<TopItem> Calculate(FileSystemNode node)
        {
            return node.Search()
                       .Where(r => r.FileType == FileType.File && r.CreationTime.HasValue)
                       .GroupBy(r => r.CreationTime.Value.Year)
                       .Select(r => new TopItem()
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