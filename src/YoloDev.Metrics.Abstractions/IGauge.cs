using YoloDev.Metrics.Abstractions;

namespace YoloDev.Metrics
{
  public interface IGauge : IMetricFamily<IGaugeMetric>, IGaugeMetric { }
}
