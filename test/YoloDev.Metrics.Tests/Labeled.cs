using System.Collections.Immutable;
using YoloDev.Metrics.Impl;

namespace YoloDev.Metrics.Tests
{
  internal static class Labeled
  {
    public static ImmutableArray<string> SingleLabels { get; } = ImmutableArray.Create("label");

    public static LabelValues Single(string value) => new LabelValues(SingleLabels, ImmutableArray.Create(value));
  }
}
