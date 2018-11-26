using System.Collections.Generic;
using System.Collections.Immutable;

namespace YoloDev.Metrics.Abstractions
{
  public interface IMetricVisitor<T>
  {
    T VisitCollection(IEnumerable<IMetricFamily> metrics);
    T VisitFamily(string name, string help, MetricType type, IEnumerable<IMetric> metrics);
    T VisitCounter(double value, IEnumerable<(string name, string value)> labels);
    T VisitGauge(double value, IEnumerable<(string name, string value)> labels);
    T VisitSummary(long sampleCount, double sampleSum, IEnumerable<(double quantile, double value)> samples, IEnumerable<(string name, string value)> labels);
    T VisitHistogram(long sampleCount, double sampleSum, IEnumerable<(long cumulativeCount, double upperBound)> buckets, IEnumerable<(string name, string value)> labels);
  }
}
