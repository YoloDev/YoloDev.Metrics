using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using YoloDev.Metrics.Abstractions;
using YoloDev.Metrics.Abstractions.Options;

namespace YoloDev.Metrics.Impl
{
  public class DefaultHistogram : DefaultMetricFamily<IHistogramMetric>, IHistogram
  {
    internal static DefaultHistogram Factory(string name, Action<HistogramOptions> init)
    {
      var options = new HistogramOptions();
      init(options);
      return new DefaultHistogram(name, options);
    }

    private DefaultHistogram(string name, MetricOptions options, ImmutableArray<double> buckets)
      : base(name, MetricType.Histogram, options, labels => new HistogramMetric(labels, buckets))
    {
      Guard.InvalidLabel("Histogram", LabelNames, "le");
    }

    public DefaultHistogram(string name, HistogramOptions options)
      : this(name, options, MaybeAddInfinity(options.Buckets).ToImmutableArray())
    {
    }

    private static ImmutableArray<double> MaybeAddInfinity(IEnumerable<double> buckets)
    {
      // TODO: Validate that buckets are sorted?
      var list = buckets.ToImmutableList();
      if (list[list.Count - 1] != Double.PositiveInfinity)
      {
        list = list.Add(Double.PositiveInfinity);
      }

      return list.ToImmutableArray();
    }

    public void Observe(double value)
    {
      Unlabeled.Observe(value);
    }

    private class HistogramMetric : IHistogramMetric
    {
      readonly LabelValues _labels;
      readonly ImmutableArray<double> _upperBounds;
      readonly AtomicLong[] _cumulativeCounts;
      readonly AtomicDouble _sum = new AtomicDouble(0);

      public HistogramMetric(LabelValues labels, ImmutableArray<double> buckets)
      {
        _labels = labels;
        _upperBounds = buckets;
        _cumulativeCounts = new AtomicLong[buckets.Length];

        for (int i = 0; i < buckets.Length; ++i)
        {
          _cumulativeCounts[i] = new AtomicLong(0);
        }
      }

      public void Observe(double value)
      {
        for (int i = 0; i < _upperBounds.Length; ++i)
        {
          // The last bucket is +Inf, so we always increment.
          if (value <= _upperBounds[i])
          {
            _cumulativeCounts[i].Increment(1);
            break;
          }
        }

        _sum.Increment(value);
      }

      public T Visit<T>(IMetricVisitor<T> visitor)
      {
        var buckets = new (long cumulativeCount, double upperBound)[_cumulativeCounts.Length];
        var acc = 0L;
        for (int i = 0; i < _cumulativeCounts.Length; ++i)
        {
          acc += _cumulativeCounts[i].Value;
          buckets[i] = (acc, _upperBounds[i]);
        }

        var sum = _sum.Value;
        return visitor.VisitHistogram(acc, sum, buckets, _labels);
      }
    }
  }
}
