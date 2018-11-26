namespace YoloDev.Metrics.Abstractions
{
  public interface IHistogramMetric : IMetric
  {
    void Observe(double value);
  }
}
