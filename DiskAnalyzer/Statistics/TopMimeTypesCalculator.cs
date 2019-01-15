using System.Collections.Generic;
using System.Linq;
using System.Web;
using DiskAnalyzer.Model;

namespace DiskAnalyzer.Statistics
{
    public class TopMimeTypesCalculator : IStatisticsCalculator
    {
        public IEnumerable<TopItem> Calculate(FileSystemNode node)
        {
            return node.Search()
                       .Where(r => r.FileType == FileType.File)
                       .OrderByDescending(a => a.Size)
                       .GroupBy(r => MimeMapping.GetMimeMapping(r.Name))
                       .Select(r => new TopItem()
                                    {
                                        Name = r.Key,
                                        Path = r.First().GetFullPath(),
                                        Size = r.Sum(n => n.Size),
                                        CountFiles = r.Count(),
                                    })
                       .OrderByDescending(a => a.Size);
        }
    }
}