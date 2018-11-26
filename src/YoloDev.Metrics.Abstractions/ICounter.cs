using YoloDev.Metrics.Abstractions;

namespace YoloDev.Metrics
{
  public interface ICounter : IMetricFamily<ICounterMetric>, ICounterMetric { }
}
