using YoloDev.Metrics.Abstractions;

namespace YoloDev.Metrics
{
  public interface IMetric
  {
    T Visit<T>(IMetricVisitor<T> visitor);
  }
}
