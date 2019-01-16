using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiskAnalyzer.Model;

namespace DiskAnalyzer.Statistics
{
    public class TopExtensionsCalculator : IStatisticsCalculator
    {
        public IEnumerable<StatisticsItem> Calculate(IFileSystemNode node)
        {
            return node.Search()
                       .Where(r => r.FileType == FileType.File)
                       .OrderByDescending(a => a.Size)
                       .GroupBy(r => Path.GetExtension(r.Name)?.ToLower())
                       .Select(r => new StatisticsItem()
                                    {
                                        Name = string.IsNullOrEmpty(r.Key) ? "*" : r.Key,
                                        Path = r.First().GetFullPath(),
                                        Size = r.Sum(n => n.Size),
                                        CountFiles = r.Count(),
                                    })
                       .OrderByDescending(a => a.Size)
                       .Take(200);
        }
    }
}