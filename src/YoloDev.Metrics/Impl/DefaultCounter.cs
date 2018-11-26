using System;
using YoloDev.Metrics.Abstractions;
using YoloDev.Metrics.Abstractions.Options;

namespace YoloDev.Metrics.Impl
{
  public class DefaultCounter : DefaultMetricFamily<ICounterMetric>, ICounter
  {
    internal static DefaultCounter Factory(string name, Action<CounterOptions> init)
    {
      var options = new CounterOptions();
      init(options);
      return new DefaultCounter(name, options);
    }

    public DefaultCounter(string name, CounterOptions options)
      : base(name, MetricType.Counter, options, labels => new CounterMetric(labels))
    {
    }

    public void Increment(double value = 1)
    {
      Unlabeled.Increment(value);
    }

    private class CounterMetric : ICounterMetric
    {
      readonly LabelValues _labels;
      readonly AtomicDouble _value;

      public CounterMetric(LabelValues labels)
      {
        _labels = labels;
        _value = new AtomicDouble(0);
      }

      public void Increment(double value = 1)
      {
        if (value < 0)
        {
          throw new ArgumentOutOfRangeException(nameof(value), value, "value must be positive");
        }

        _value.Increment(value);
      }

      public T Visit<T>(IMetricVisitor<T> visitor)
        => visitor.VisitCounter(_value.Value, _labels);
    }
  }
}
