using System;
using YoloDev.Metrics.Abstractions.Options;

namespace YoloDev.Metrics.Abstractions
{
  public interface IMetricFactory
  {
    ICounter CreateCounter(string name, Action<CounterOptions> init);
    IGauge CreateGauge(string name, Action<GaugeOptions> init);
    ISummary CreateSummary(string name, Action<SummaryOptions> init);
    IHistogram CreateHistogram(string name, Action<HistogramOptions> init);
  }
}
