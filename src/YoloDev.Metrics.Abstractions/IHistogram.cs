using YoloDev.Metrics.Abstractions;

namespace YoloDev.Metrics
{
  public interface IHistogram : IMetricFamily<IHistogramMetric>, IHistogramMetric { }
}
