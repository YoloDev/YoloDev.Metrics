using System;
using System.Collections.Immutable;
using NodaTime;
using YoloDev.Metrics.Abstractions;
using YoloDev.Metrics.Abstractions.Options;

namespace YoloDev.Metrics.Impl
{
  public class DefaultMetricFactory : IMetricFactory
  {
    readonly IClock _clock;
    readonly IMetricRegistry _registry;

    public DefaultMetricFactory(IClock clock, IMetricRegistry registry)
    {
      Guard.ArgNotNull(clock, nameof(clock));
      Guard.ArgNotNull(registry, nameof(registry));

      _clock = clock;
      _registry = registry;
    }

    public virtual ICounter CreateCounter(string name, Action<CounterOptions> init)
      => Create<ICounter>.Metric(_registry, name, DefaultCounter.Factory, init);

    public virtual IGauge CreateGauge(string name, Action<GaugeOptions> init)
      => Create<IGauge>.Metric(_registry, name, DefaultGauge.Factory, init);

    public virtual IHistogram CreateHistogram(string name, Action<HistogramOptions> init)
      => Create<IHistogram>.Metric(_registry, name, DefaultHistogram.Factory, init);

    public ISummary CreateSummary(string name, Action<SummaryOptions> init)
      => Create<ISummary>.Metric(_registry, name, DefaultSummary.Factory, (init, _clock));

    static class Create<T> where T : IMetricFamily
    {
      public static T Metric<TArg>(IMetricRegistry registry, string name, Func<string, TArg, IMetricFamily> factory, TArg arg)
      {
        var metric = registry.GetOrAdd(name, factory, arg);
        if (metric is T m)
        {
          return m;
        }

        throw new InvalidOperationException($"Metric {name} is already created with a different type {metric.GetType().Name}");
      }
    }
  }

  public class DefaultMetricFactory<T> : IMetricFactory<T>
  {
    readonly IMetricFactory _factory;
    ImmutableDictionary<string, ICounter> _counters = ImmutableDictionary.Create<string, ICounter>();
    ImmutableDictionary<string, IGauge> _gauges = ImmutableDictionary.Create<string, IGauge>();
    ImmutableDictionary<string, IHistogram> _histograms = ImmutableDictionary.Create<string, IHistogram>();
    ImmutableDictionary<string, ISummary> _summaries = ImmutableDictionary.Create<string, ISummary>();

    public DefaultMetricFactory(IMetricFactory factory)
    {
      _factory = factory;
    }

    public ICounter CreateCounter(string name, Action<CounterOptions> init)
      => ImmutableInterlocked.GetOrAdd(ref _counters, name, MakeCounter, init);

    public IGauge CreateGauge(string name, Action<GaugeOptions> init)
      => ImmutableInterlocked.GetOrAdd(ref _gauges, name, MakeGauge, init);

    public IHistogram CreateHistogram(string name, Action<HistogramOptions> init)
      => ImmutableInterlocked.GetOrAdd(ref _histograms, name, MakeHistogram, init);

    public ISummary CreateSummary(string name, Action<SummaryOptions> init)
      => ImmutableInterlocked.GetOrAdd(ref _summaries, name, MakeSummary, init);

    ICounter MakeCounter(string name, Action<CounterOptions> init)
      => _factory.CreateCounter(name, init);

    IGauge MakeGauge(string name, Action<GaugeOptions> init)
      => _factory.CreateGauge(name, init);

    IHistogram MakeHistogram(string name, Action<HistogramOptions> init)
      => _factory.CreateHistogram(name, init);

    ISummary MakeSummary(string name, Action<SummaryOptions> init)
      => _factory.CreateSummary(name, init);
  }
}
