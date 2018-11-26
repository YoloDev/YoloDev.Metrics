using System;

namespace YoloDev.Metrics.Abstractions
{
  public interface IMetricRegistry
  {
    IMetric GetOrAdd<TArg>(string name, Func<string, TArg, IMetricFamily> factory, TArg factoryArg);
    bool Remove(IMetricFamily metric);
    T Visit<T>(IMetricVisitor<T> visitor);
  }
}
