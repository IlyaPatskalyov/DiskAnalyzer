using System.Collections.Generic;
using System.Linq;
using DiskAnalyzer.Core;
using DiskAnalyzer.Model;

namespace DiskAnalyzer.Statistics
{
    public class TopOwnersCalculator : IStatisticsCalculator
    {
        public IEnumerable<TopItem> Calculate(FileSystemNode node)
        {
            return node.Search()
                       .Where(r => r.FileType == FileType.File)
                       .GroupBy(r => IoHelpers.GetOwner(r.GetFullPath()))
                       .Select(r => new TopItem()
                                    {
                                        Name = r.Key ?? "Unknown",
                                        Size = r.Sum(n => n.Size),
                                        CountFiles = r.Count(),
                                    })
                       .OrderByDescending(a => a.Size);
        }
    }
}