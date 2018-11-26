using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using YoloDev.Metrics.Abstractions;
using YoloDev.Metrics.Abstractions.Options;

namespace YoloDev.Metrics.Impl
{
  public abstract class DefaultMetricFamily<TMetric> : IMetricFamily
    where TMetric : class, IMetric
  {
    readonly Func<LabelValues, TMetric> _factory;

    ImmutableDictionary<LabelValues, TMetric> _metrics = ImmutableDictionary.Create<LabelValues, TMetric>();

    internal DefaultMetricFamily(string name, MetricType type, MetricOptions options, Func<LabelValues, TMetric> factory)
    {
      Guard.ArgNotNull(factory, nameof(factory));
      Guard.ValidMetricName(name, nameof(name));
      var labels = Guard.ValidLabelNames(options.LabelNames);

      _factory = factory;
      Name = name;
      Type = type;
      Help = options.Help;
      LabelNames = labels;
      SupressInitialValue = options.SuppressInitialValue;
    }

    public string Name { get; }

    protected MetricType Type { get; }
    protected string Help { get; }
    protected ImmutableArray<string> LabelNames { get; }
    protected bool SupressInitialValue { get; }
    protected ImmutableDictionary<LabelValues, TMetric> Metrics => _metrics;

    internal TMetric Unlabeled => WithLabels();

    public TMetric WithLabels(params string[] values)
    {
      if (values.Length != LabelNames.Length)
      {
        throw new ArgumentException($"The provided values does not have the same length as the expected labels. Labels: {LabelNames.Length}");
      }

      var labelValues = new LabelValues(LabelNames, values.ToImmutableArray());
      return ImmutableInterlocked.GetOrAdd(ref _metrics, labelValues, _factory);
    }

    public virtual T Visit<T>(IMetricVisitor<T> visitor)
    {
      if (!SupressInitialValue && LabelNames.Length == 0)
      {
        // Ensure non-labeled metric is created
        WithLabels();
      }

      return visitor.VisitFamily(Name, Help, Type, Metrics.Values.Cast<IMetric>());
    }
  }
}
