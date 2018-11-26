namespace YoloDev.Metrics.Abstractions
{
  public interface IGaugeMetric : IMetric
  {
    void Increment(double value = 1);
    void Decrement(double value = 1);
    double Value { set; }
  }
}
