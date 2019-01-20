using System.Collections.Generic;
using System.Threading;
using DiskAnalyzer.Model;
using JetBrains.Annotations;

namespace DiskAnalyzer.Statistics
{
    public interface IStatisticsCalculator
    {
        [NotNull]
        IEnumerable<StatisticsItem> Calculate([NotNull] IFileSystemNode node, CancellationToken token);
    }
}