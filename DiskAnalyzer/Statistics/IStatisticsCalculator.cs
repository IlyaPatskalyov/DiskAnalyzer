using System.Collections.Generic;
using DiskAnalyzer.Model;

namespace DiskAnalyzer.Statistics
{
    public interface IStatisticsCalculator
    {
        IEnumerable<TopItem> Calculate(FileSystemNode node);
    }
}