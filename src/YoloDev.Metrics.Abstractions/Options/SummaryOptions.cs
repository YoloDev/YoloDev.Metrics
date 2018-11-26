using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace YoloDev.Metrics.Abstractions.Options
{
  public class SummaryOptions : MetricOptions
  {
    static readonly ImmutableArray<SummaryObjective> DefaultObjectives
      = ImmutableArray.Create(
        new SummaryObjective(0.5, 0.05),
        new SummaryObjective(0.9, 0.01),
        new SummaryObjective(0.99, 0.001));

    public IEnumerable<SummaryObjective> Objectives { get; set; } = DefaultObjectives;
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(10);
    public int AgeBuckets { get; set; } = 5;
  }
}
