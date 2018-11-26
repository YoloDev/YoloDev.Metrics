using System.Collections.Generic;

namespace YoloDev.Metrics.Abstractions.Options
{
  public class MetricOptions
  {
    public string Help { get; set; }
    public IEnumerable<string> LabelNames { get; set; }
    public bool SuppressInitialValue { get; set; }
  }
}
