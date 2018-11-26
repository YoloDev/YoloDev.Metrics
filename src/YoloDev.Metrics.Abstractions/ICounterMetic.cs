namespace YoloDev.Metrics.Abstractions
{
  public interface ICounterMetric : IMetric
  {
    void Increment(double value = 1);
  }
}
