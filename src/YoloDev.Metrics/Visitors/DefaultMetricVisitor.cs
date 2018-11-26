using System.Collections.Generic;
using YoloDev.Metrics.Abstractions;

namespace YoloDev.Metrics.Visitors
{
  public class DefaultMetricVisitor : IMetricVisitor<object>
  {
    public object VisitCollection(IEnumerable<IMetricFamily> metrics)
    {
      foreach (var metric in metrics)
      {
        metric.Visit(this);
      }

      return null;
    }

    public virtual object VisitCounter(double value, IEnumerable<(string name, string value)> labels)
    {
      return null;
    }

    public virtual object VisitFamily(string name, string help, MetricType type, IEnumerable<IMetric> metrics)
    {
      foreach (var metric in metrics)
      {
        metric.Visit(this);
      }

      return null;
    }

    public virtual object VisitGauge(double value, IEnumerable<(string name, string value)> labels)
    {
      return null;
    }

    public virtual object VisitHistogram(long sampleCount, double sampleSum, IEnumerable<(long cumulativeCount, double upperBound)> buckets, IEnumerable<(string name, string value)> labels)
    {
      return null;
    }

    public virtual object VisitSummary(long sampleCount, double sampleSum, IEnumerable<(double quantile, double value)> quantile, IEnumerable<(string name, string value)> labels)
    {
      return null;
    }
  }
}
