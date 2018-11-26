using System;
using YoloDev.Metrics.Abstractions;
using YoloDev.Metrics.Abstractions.Options;

namespace YoloDev.Metrics.Impl
{
  public class DefaultGauge : DefaultMetricFamily<IGaugeMetric>, IGauge
  {
    internal static DefaultGauge Factory(string name, Action<GaugeOptions> init)
    {
      var options = new GaugeOptions();
      init(options);
      return new DefaultGauge(name, options);
    }

    public DefaultGauge(string name, GaugeOptions options)
      : base(name, MetricType.Gauge, options, labels => new GaugeMetric(labels))
    {
    }

    public double Value { set => Unlabeled.Value = value; }

    public void Decrement(double value = 1)
    {
      Unlabeled.Decrement(value);
    }

    public void Increment(double value = 1)
    {
      Unlabeled.Increment(value);
    }

    private class GaugeMetric : IGaugeMetric
    {
      readonly LabelValues _labels;
      readonly AtomicDouble _value;

      public GaugeMetric(LabelValues labels)
      {
        _labels = labels;
        _value = new AtomicDouble(0);
      }

      public double Value { set => _value.SetValue(value); }

      public void Decrement(double value = 1)
      {
        _value.Decrement(value);
      }

      public void Increment(double value = 1)
      {
        _value.Increment(value);
      }

      public T Visit<T>(IMetricVisitor<T> visitor)
        => visitor.VisitGauge(_value.Value, _labels);
    }
  }
}
