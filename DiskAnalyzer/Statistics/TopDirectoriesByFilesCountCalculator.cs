using System.Collections.Generic;
using System.Linq;
using DiskAnalyzer.Model;

namespace DiskAnalyzer.Statistics
{
    public class TopDirectoriesByFilesCountCalculator : IStatisticsCalculator
    {
        public IEnumerable<TopItem> Calculate(FileSystemNode node)
        {
            return node.Search()
                       .Where(r => r.FileType == FileType.Directory &&
                                   0.95 * r.CountFiles > r
                                                         .Children.Where(z => z.FileType == FileType.Directory)
                                                         .Select(z => z.CountFiles)
                                                         .DefaultIfEmpty().Max())
                       .OrderByDescending(a => a.CountFiles)
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