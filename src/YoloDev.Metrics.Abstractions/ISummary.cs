using YoloDev.Metrics.Abstractions;

namespace YoloDev.Metrics
{
  public interface ISummary : IMetricFamily<ISummaryMetric>, ISummaryMetric { }
}
