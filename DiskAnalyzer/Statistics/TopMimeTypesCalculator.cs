using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using DiskAnalyzer.Model;
using JetBrains.Annotations;

namespace DiskAnalyzer.Statistics
{
    [UsedImplicitly]
    public class TopMimeTypesCalculator : IStatisticsCalculator
    {
        public IEnumerable<StatisticsItem> Calculate(IFileSystemNode node, CancellationToken token)
        {
            return node.Search(token)
                       .Where(r => r.FileType == FileType.File)
                       .OrderByDescending(a => a.Size)
                       .GroupBy(r => MimeMapping.GetMimeMapping(r.Name))
                       .Select(r => new StatisticsItem()
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