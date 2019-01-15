using System.Collections.Generic;
using System.Linq;
using DiskAnalyzer.Model;

namespace DiskAnalyzer.Statistics
{
    public class TopFilesBySizeCalculator : IStatisticsCalculator
    {
        public IEnumerable<TopItem> Calculate(FileSystemNode node)
        {
            return node.Search()
                       .Where(r => r.FileType == FileType.File)
                       .OrderByDescending(a => a.Size)
                       .Select(r => new TopItem
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