using System.Collections.Immutable;

namespace YoloDev.Metrics.Abstractions
{
  public interface IMetricFamily : IMetric
  {
    string Name { get; }
  }

  public interface IMetricFamily<TMetric> : IMetricFamily
    where TMetric : IMetric
  {
    TMetric WithLabels(params string[] values);
  }
}
