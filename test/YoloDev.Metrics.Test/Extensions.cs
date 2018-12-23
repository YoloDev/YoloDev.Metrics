using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using YoloDev.Metrics.Impl;
using YoloDev.Metrics.Visitors;

namespace YoloDev.Metrics.Test
{
  internal static class Extensions
  {
    public static CounterVisitor Collect(this ICounter counter)
    {
      var visitor = new CounterVisitor();
      counter.Visit(visitor);
      return visitor;
    }

    public static IReadOnlyDictionary<LabelValues, double> Values(this ICounter counter)
      => counter.Collect().Values;

    public static double Value(this ICounter counter)
      => counter.Values().Single().Value;

    public static GaugeVisitor Collect(this IGauge gauge)
    {
      var visitor = new GaugeVisitor();
      gauge.Visit(visitor);
      return visitor;
    }

    public static IReadOnlyDictionary<LabelValues, double> Values(this IGauge gauge)
      => gauge.Collect().Values;

    public static double Value(this IGauge gauge)
      => gauge.Values().Single().Value;

    public static HistogramVisitor Collect(this IHistogram histogram)
    {
      var visitor = new HistogramVisitor();
      histogram.Visit(visitor);
      return visitor;
    }

    public static IReadOnlyDictionary<LabelValues, HistogramValue> Values(this IHistogram histogram)
      => histogram.Collect().Values;

    public static HistogramValue Value(this IHistogram histogram)
      => histogram.Values().Single().Value;

    public static long Count(this IHistogram histogram)
      => histogram.Value().Count;

    public static double Sum(this IHistogram histogram)
      => histogram.Value().Sum;

    public static HistogramBucket Bucket(this IHistogram histogram, double b)
      => histogram.Value().Buckets.First(bucket => bucket.UpperBound == b);

    public static double BucketValue(this IHistogram histogram, double b)
      => histogram.Bucket(b).CumulativeCount;

    public static HistogramValue? Value(this IHistogram histogram, string label)
      => histogram.Values().TryGetValue(Labeled.Single(label), out HistogramValue value)
        ? value
        : (HistogramValue?)null;

    public static long? Count(this IHistogram histogram, string label)
    => histogram.Value(label)?.Count;

    public static double? Sum(this IHistogram histogram, string label)
      => histogram.Value(label)?.Sum;

    public static SummaryVisitor Collect(this ISummary summary)
    {
      var visitor = new SummaryVisitor();
      summary.Visit(visitor);
      return visitor;
    }

    public static IReadOnlyDictionary<LabelValues, SummaryValue> Values(this ISummary summary)
      => summary.Collect().Values;

    public static SummaryValue Value(this ISummary summary)
      => summary.Values().Single().Value;

    public static long Count(this ISummary summary)
      => summary.Value().Count;

    public static double Sum(this ISummary summary)
      => summary.Value().Sum;

    public static SummarySample Sample(this ISummary summary, double quantile)
      => summary.Value().Samples.First(s => s.Quantile == quantile);

    public static double SampleValue(this ISummary summary, double quantile)
      => summary.Sample(quantile).Value;

    public static SummaryValue? Value(this ISummary summary, string label)
      => summary.Values().TryGetValue(Labeled.Single(label), out SummaryValue value)
        ? value
        : (SummaryValue?)null;

    public static long? Count(this ISummary summary, string label)
      => summary.Value(label)?.Count;

    public static double? Sum(this ISummary summary, string label)
      => summary.Value(label)?.Sum;

    public static SummarySample? Sample(this ISummary summary, string label, double quantile)
      => summary.Value(label)?.Samples.First(s => s.Quantile == quantile);

    public static double? SampleValue(this ISummary summary, string label, double quantile)
      => summary.Sample(label, quantile)?.Value;

    private static LabelValues ToLabelValues(this IEnumerable<(string name, string value)> labels)
    {
      var list = labels.ToList();
      var names = list.Select(v => v.name).ToImmutableArray();
      var values = list.Select(v => v.value).ToImmutableArray();
      return new LabelValues(names, values);
    }

    public class TestVisitor : DefaultMetricVisitor
    {
      public string Name { get; private set; }
      public string Help { get; private set; }
      public MetricType Type { get; private set; }

      public override object VisitFamily(string name, string help, MetricType type, IEnumerable<IMetric> metrics)
      {
        Name = name;
        Help = help;
        Type = type;
        return base.VisitFamily(name, help, type, metrics);
      }
    }

    public class CounterVisitor : TestVisitor
    {
      public Dictionary<LabelValues, double> Values { get; } = new Dictionary<LabelValues, double>();

      public override object VisitCounter(double value, IEnumerable<(string name, string value)> labels)
      {
        Values.Add(labels.ToLabelValues(), value);
        return base.VisitCounter(value, labels);
      }
    }

    public class GaugeVisitor : TestVisitor
    {
      public Dictionary<LabelValues, double> Values { get; } = new Dictionary<LabelValues, double>();

      public override object VisitGauge(double value, IEnumerable<(string name, string value)> labels)
      {
        Values.Add(labels.ToLabelValues(), value);
        return base.VisitGauge(value, labels);
      }
    }

    public class HistogramVisitor : TestVisitor
    {
      public Dictionary<LabelValues, HistogramValue> Values { get; } =
        new Dictionary<LabelValues, HistogramValue>();

      public override object VisitHistogram(long sampleCount, double sampleSum, IEnumerable<(long cumulativeCount, double upperBound)> buckets, IEnumerable<(string name, string value)> labels)
      {
        Values.Add(labels.ToLabelValues(), new HistogramValue(sampleCount, sampleSum, buckets));
        return base.VisitHistogram(sampleCount, sampleSum, buckets, labels);
      }
    }

    public class SummaryVisitor : TestVisitor
    {
      public Dictionary<LabelValues, SummaryValue> Values { get; } =
        new Dictionary<LabelValues, SummaryValue>();

      public override object VisitSummary(long sampleCount, double sampleSum, IEnumerable<(double quantile, double value)> samples, IEnumerable<(string name, string value)> labels)
      {
        Values.Add(labels.ToLabelValues(), new SummaryValue(sampleCount, sampleSum, samples));
        return base.VisitSummary(sampleCount, sampleSum, samples, labels);
      }
    }

    public readonly struct HistogramValue
    {
      public long Count { get; }
      public double Sum { get; }
      public List<HistogramBucket> Buckets { get; }

      public HistogramValue(long sampleCount, double sampleSum, IEnumerable<(long cumulativeCount, double upperBound)> buckets)
      {
        Count = sampleCount;
        Sum = sampleSum;
        Buckets = buckets.Select(HistogramBucket.Create).ToList();
      }
    }

    public readonly struct HistogramBucket
    {
      public long CumulativeCount { get; }
      public double UpperBound { get; }

      public HistogramBucket(long cumulativeCount, double upperBound)
      {
        CumulativeCount = cumulativeCount;
        UpperBound = upperBound;
      }

      public static HistogramBucket Create((long cumulativeCount, double upperBound) tpl)
        => new HistogramBucket(tpl.cumulativeCount, tpl.upperBound);
    }

    public readonly struct SummaryValue
    {
      public long Count { get; }
      public double Sum { get; }
      public List<SummarySample> Samples { get; }

      public SummaryValue(long sampleCount, double sampleSum, IEnumerable<(double quantile, double value)> samples)
      {
        Count = sampleCount;
        Sum = sampleSum;
        Samples = samples.Select(SummarySample.Create).ToList();
      }
    }

    public readonly struct SummarySample
    {
      public double Quantile { get; }
      public double Value { get; }

      public SummarySample(double quantile, double value)
      {
        Quantile = quantile;
        Value = value;
      }

      public static SummarySample Create((double quantile, double value) tpl)
        => new SummarySample(tpl.quantile, tpl.value);
    }
  }
}
