using System;
using System.Collections.Generic;
using NodaTime;
using NodaTime.Testing;
using Xunit;
using YoloDev.Metrics.Abstractions.Options;
using YoloDev.Metrics.Impl;

namespace YoloDev.Metrics.Tests
{
  public class SummaryTests
  {
    readonly FakeClock clock;
    readonly ISummary noLabel;
    readonly ISummary labeled;
    readonly ISummary noLabelAndQuantiles;
    readonly ISummary labelAndQuantiles;

    public SummaryTests()
    {
      clock = FakeClock.FromUtc(2020, 10, 20);

      noLabel = new DefaultSummary("noLabel", new SummaryOptions
      {
        Help = "help noLabels",
      }, clock);

      labeled = new DefaultSummary("labeled", new SummaryOptions
      {
        Help = "help labeled",
        LabelNames = Labeled.SingleLabels
      }, clock);

      noLabelAndQuantiles = new DefaultSummary("no_labels_and_quantiles", new SummaryOptions
      {
        Help = "help noLabelAndQuantiles",
        Objectives = new[] {
          new SummaryObjective(0.5, 0.05),
          new SummaryObjective(0.9, 0.01),
          new SummaryObjective(0.99, 0.001)
        }
      }, clock);

      labelAndQuantiles = new DefaultSummary("labels_and_quantiles", new SummaryOptions
      {
        Help = "help labelAndQuantiles",
        LabelNames = Labeled.SingleLabels,
        Objectives = new[] {
          new SummaryObjective(0.5, 0.05),
          new SummaryObjective(0.9, 0.01),
          new SummaryObjective(0.99, 0.001)
        }
      }, clock);
    }

    [Fact]
    public void TestObserve()
    {
      noLabel.Observe(2);
      Assert.Equal(1, noLabel.Count());
      Assert.Equal(2, noLabel.Sum());
      noLabel.WithLabels().Observe(4);
      Assert.Equal(2, noLabel.Count());
      Assert.Equal(6, noLabel.Sum());
    }

    [Fact]
    public void TestQuantiles()
    {
      var nSamples = 1000000; // simulate one million samples

      for (var i = 1; i < nSamples; i++)
      {
        // In this test, we observe the numbers from 1 to nSamples,
        // because that makes it easy to verify if the quantiles are correct.
        labelAndQuantiles.WithLabels("a").Observe(i);
        noLabelAndQuantiles.Observe(i);
      }

      Assert.Equal(0.5 * nSamples, noLabelAndQuantiles.SampleValue(0.5), new DeltaComparer(0.05 * nSamples));
      Assert.Equal(0.9 * nSamples, noLabelAndQuantiles.SampleValue(0.9), new DeltaComparer(0.01 * nSamples));
      Assert.Equal(0.99 * nSamples, noLabelAndQuantiles.SampleValue(0.99), new DeltaComparer(0.001 * nSamples));

      Assert.Equal(0.5 * nSamples, labelAndQuantiles.SampleValue("a", 0.5), new DeltaComparer(0.05 * nSamples));
      Assert.Equal(0.9 * nSamples, labelAndQuantiles.SampleValue("a", 0.9), new DeltaComparer(0.01 * nSamples));
      Assert.Equal(0.99 * nSamples, labelAndQuantiles.SampleValue("a", 0.99), new DeltaComparer(0.001 * nSamples));
    }

    [Fact]
    public void TestMaxAge()
    {
      var summary = new DefaultSummary("s", new SummaryOptions
      {
        Objectives = new[] {
          new SummaryObjective(0.99, 0.001)
        },
        MaxAge = TimeSpan.FromSeconds(1), // After 1s, all observations will be discarded.
        AgeBuckets = 2 // We got 2 buckets, so we discard one bucket every 500ms.
      }, clock);

      summary.Observe(8);
      Assert.Equal(8, summary.SampleValue(0.99)); // From bucket 1.
      clock.AdvanceMilliseconds(600);
      Assert.Equal(8, summary.SampleValue(0.99)); // From bucket 2.
      clock.AdvanceMilliseconds(600);
      Assert.Equal(Double.NaN, summary.SampleValue(0.99)); // Bucket 1 again, now it is empty.
    }

    [Fact]
    public void DefaultZeroValue()
    {
      Assert.Equal(0, noLabel.Count());
      Assert.Equal(0, noLabel.Sum());

      labeled.WithLabels("1");
      labeled.WithLabels("2");
      var values = labeled.Values();
      Assert.Equal(2, values.Count);
      foreach (var v in values.Values)
      {
        Assert.Equal(0, v.Count);
        Assert.Equal(0, v.Sum);
      }
    }

    [Fact]
    public void TestLabels()
    {
      Assert.Null(labeled.Count("a"));
      Assert.Null(labeled.Sum("a"));
      Assert.Null(labeled.Count("b"));
      Assert.Null(labeled.Sum("b"));

      labeled.WithLabels("a").Observe(2);
      Assert.Equal(1, labeled.Count("a"));
      Assert.Equal(2, labeled.Sum("a"));
      Assert.Null(labeled.Count("b"));
      Assert.Null(labeled.Sum("b"));

      labeled.WithLabels("b").Observe(3);
      Assert.Equal(1, labeled.Count("a"));
      Assert.Equal(2, labeled.Sum("a"));
      Assert.Equal(1, labeled.Count("b"));
      Assert.Equal(3, labeled.Sum("b"));
    }

    [Fact]
    public void QuantileLabelThrows()
    {
      Assert.Throws<ArgumentException>(() => new DefaultSummary("quantile", new SummaryOptions
      {
        LabelNames = new[] { "quantile" }
      }, clock));

      Assert.Throws<ArgumentException>(() => new DefaultSummary("quantile", new SummaryOptions
      {
        LabelNames = new[] { "foo", "bar", "quantile" }
      }, clock));
    }

    [Fact]
    public void Visit()
    {
      var noLabelInfo = noLabel.Collect();
      var labeledInfo = labeled.Collect();

      Assert.Equal("noLabel", noLabelInfo.Name);
      Assert.Equal("help noLabels", noLabelInfo.Help);
      Assert.Equal(MetricType.Summary, noLabelInfo.Type);

      Assert.Equal("labeled", labeledInfo.Name);
      Assert.Equal("help labeled", labeledInfo.Help);
      Assert.Equal(MetricType.Summary, labeledInfo.Type);
    }

    class DeltaComparer : IEqualityComparer<double>, IEqualityComparer<double?>
    {
      public double Delta { get; }

      public DeltaComparer(double delta)
      {
        Delta = delta;
      }

      public bool Equals(double x, double y)
        => Math.Abs(x - y) < Delta;

      public int GetHashCode(double x)
        => x.GetHashCode();

      public bool Equals(double? x, double? y)
        => x == y || (x != null && y != null && Equals(x.Value, y.Value));

      public int GetHashCode(double? obj)
        => obj == null ? 0 : GetHashCode(obj.Value);
    }
  }
}
