using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiskAnalyzer.Model;

namespace DiskAnalyzer.Statistics
{
    public class TopExtensionsCalculator : IStatisticsCalculator
    {
        public IEnumerable<TopItem> Calculate(FileSystemNode node)
        {
            return node.Search()
                       .Where(r => r.FileType == FileType.File)
                       .GroupBy(r => Path.GetExtension(r.Name)?.ToLower())
                       .Select(r => new TopItem()
                                    {
                                        Name = string.IsNullOrEmpty(r.Key) ? "*" : r.Key,
                                        Size = r.Sum(n => n.Size),
                                        CountFiles = r.Count(),
                                    })
                       .OrderByDescending(a => a.Size);
        }
    }
}