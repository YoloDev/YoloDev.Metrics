namespace YoloDev.Metrics.Abstractions
{
  public interface ISummaryMetric : IMetric
  {
    void Observe(double value);
  }
}
