using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using NodaTime;
using YoloDev.Metrics.Abstractions;
using YoloDev.Metrics.Abstractions.Options;
using static YoloDev.Metrics.Impl.CKMSQuantiles;

namespace YoloDev.Metrics.Impl
{
  public class DefaultSummary : DefaultMetricFamily<ISummaryMetric>, ISummary
  {
    internal static DefaultSummary Factory(string name, (Action<SummaryOptions> init, IClock clock) args)
    {
      var options = new SummaryOptions();
      args.init(options);
      return new DefaultSummary(name, options, args.clock);
    }

    private DefaultSummary(
      string name,
      MetricOptions options,
      ImmutableArray<Quantile> objectives,
      Duration maxAge,
      int ageBuckets,
      IClock clock)
      : base(name, MetricType.Summary, options, labels => new SummaryMetric(
        clock, labels, objectives, maxAge, ageBuckets))
    {
      Guard.InvalidLabel("Summary", LabelNames, "quantile");
    }

    public DefaultSummary(string name, SummaryOptions options, IClock clock)
      : this(name, options, options.Objectives.Select(Quantile.FromQuantileEpsilonPair).ToImmutableArray(),
             Duration.FromTimeSpan(options.MaxAge), options.AgeBuckets, clock)
    {
    }

    public void Observe(double value)
    {
      Unlabeled.Observe(value);
    }

    class SummaryMetric : ISummaryMetric
    {
      readonly LabelValues _labels;
      readonly ImmutableArray<Quantile> _quantiles;

      // Having these separate leaves us open to races,
      // however Prometheus as whole has other races
      // that mean adding atomicity here wouldn't be useful.
      // This should be reevaluated in the future.
      readonly AtomicLong _count = new AtomicLong(0);
      readonly AtomicDouble _sum = new AtomicDouble(0);
      readonly TimeWindowQuantiles _quantileValues;


      public SummaryMetric(
        IClock clock,
        LabelValues labels,
        ImmutableArray<Quantile> quantiles,
        Duration maxAge,
        int ageBuckets)
      {
        _labels = labels;
        _quantiles = quantiles;
        if (_quantiles.Length > 0)
        {
          _quantileValues = new TimeWindowQuantiles(clock, quantiles, maxAge, ageBuckets);
        }
        else
        {
          _quantileValues = null;
        }
      }

      public void Observe(double value)
      {
        _count.Increment(1);
        _sum.Increment(value);
        _quantileValues?.Insert(value);
      }

      public T Visit<T>(IMetricVisitor<T> visitor)
      {
        var v = new Value(_count.Value, _sum.Value, _quantiles, _quantileValues);
        return visitor.VisitSummary(v.Count, v.Sum, v.Quantiles, _labels);
      }
    }

    readonly struct Value
    {
      public long Count { get; }
      public double Sum { get; }
      public IEnumerable<(double quantile, double value)> Quantiles { get; }

      public Value(long count, double sum, ImmutableArray<Quantile> quantiles, TimeWindowQuantiles quantileValues)
      {
        Count = count;
        Sum = sum;
        Quantiles = Snapshot(quantiles, quantileValues);
      }

      static IEnumerable<(double quantile, double value)> Snapshot(ImmutableArray<Quantile> quantiles, TimeWindowQuantiles quantileValues)
      {
        var dict = new SortedDictionary<double, double>();
        foreach (var q in quantiles)
        {
          dict.Add(q.Quant, quantileValues[q.Quant]);
        }

        return dict.Select(kvp =>
        {
          var (quantile, value) = kvp;
          return (quantile, value);
        });
      }
    }
  }
}
