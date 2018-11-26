using System.Collections.Generic;

namespace YoloDev.Metrics.Abstractions.Options
{
  public class HistogramOptions : MetricOptions
  {
    public IEnumerable<double> Buckets { get; set; } =
      new[] { .005, .01, .025, .05, .075, .1, .25, .5, .75, 1, 2.5, 5, 7.5, 10 };
  }
}
