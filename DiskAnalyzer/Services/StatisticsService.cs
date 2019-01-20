using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DiskAnalyzer.Model;
using DiskAnalyzer.Statistics;
using JetBrains.Annotations;

namespace DiskAnalyzer.Services
{
    [UsedImplicitly]
    public class StatisticsService : IStatisticsService
    {
        private readonly Dictionary<string, Basket> baskets;

        public StatisticsService(IStatisticsCalculator[] calculators)
        {
            baskets = calculators.Select(c => new Basket
                                              {
                                                  Observable = new ObservableCollection<StatisticsItem>(),
                                                  Calculator = c,
                                                  Name = c.GetType().Name.Replace("Calculator", "")
                                              })
                                 .ToDictionary(c => c.Name);
        }

        public ObservableCollection<StatisticsItem> GetCollection(string name)
        {
            return baskets.TryGetValue(name, out var pack) ? pack.Observable : null;
        }

        public void Cleanup()
        {
            foreach (var p in baskets)
                p.Value.Observable.Clear();
        }

        public Task CalculateAsync(IFileSystemNode node, CancellationToken token, SynchronizationContext synchronizationContext)
        {
            return Task.WhenAll(baskets.Select(b => CalculatePackAsync(node, b.Value, token, synchronizationContext)));
        }

        private Task CalculatePackAsync(IFileSystemNode root, Basket basket,
                                        CancellationToken token,
                                        SynchronizationContext synchronizationContext)
        {
            basket.Observable.Clear();
            return Task.Run(() => basket.Calculator.Calculate(root, token).ToList(), token)
                       .ContinueWith(
                           t => synchronizationContext.Send(v => SetCollection(basket.Observable, (IEnumerable<StatisticsItem>) v), t.Result),
                           token);
        }

        private static void SetCollection<T>(ObservableCollection<T> observable, IEnumerable<T> result)
        {
            observable.Clear();
            foreach (var r in result) observable.Add(r);
        }


        private class Basket
        {
            public IStatisticsCalculator Calculator;

            public string Name;

            public ObservableCollection<StatisticsItem> Observable;
        }
    }
}