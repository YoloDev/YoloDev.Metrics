using System;
using System.Collections.Generic;
using Xunit;
using YoloDev.Metrics.Abstractions.Options;
using YoloDev.Metrics.Impl;

namespace YoloDev.Metrics.Tests
{
  public class GaugeTests
  {
    readonly IGauge noLabel;
    readonly IGauge labeled;

    public GaugeTests()
    {
      noLabel = new DefaultGauge("noLabel", new GaugeOptions
      {
        Help = "help noLabels",
      });

      labeled = new DefaultGauge("labeled", new GaugeOptions
      {
        Help = "help labeled",
        LabelNames = Labeled.SingleLabels
      });
    }

    [Fact]
    public void Increments()
    {
      noLabel.Increment();
      Assert.Equal(1, noLabel.Value());

      noLabel.Increment(2);
      Assert.Equal(3, noLabel.Value());

      noLabel.WithLabels().Increment(4);
      Assert.Equal(7, noLabel.Value());

      noLabel.WithLabels().Increment();
      Assert.Equal(8, noLabel.Value());
    }

    [Fact]
    public void Decrement()
    {
      noLabel.Decrement();
      Assert.Equal(-1, noLabel.Value());

      noLabel.Decrement(2);
      Assert.Equal(-3, noLabel.Value());

      noLabel.WithLabels().Decrement(4);
      Assert.Equal(-7, noLabel.Value());

      noLabel.WithLabels().Decrement();
      Assert.Equal(-8, noLabel.Value());
    }

    [Fact]
    public void SetValue()
    {
      noLabel.Value = -1;
      Assert.Equal(-1, noLabel.Value());

      noLabel.Value = 3;
      Assert.Equal(3, noLabel.Value());

      noLabel.WithLabels().Value = -7;
      Assert.Equal(-7, noLabel.Value());

      noLabel.WithLabels().Value = 8;
      Assert.Equal(8, noLabel.Value());
    }

    [Fact]
    public void DefaultZero()
    {
      Assert.Equal(0, noLabel.Value());

      labeled.WithLabels("1");
      labeled.WithLabels("2");
      Assert.Equal(new Dictionary<LabelValues, double> {
        { Labeled.Single("1"), 0 },
        { Labeled.Single("2"), 0 },
      }, labeled.Values());
    }

    [Fact]
    public void WithLabels()
    {
      Assert.DoesNotContain(Labeled.Single("1"), labeled.Values());
      Assert.DoesNotContain(Labeled.Single("2"), labeled.Values());

      labeled.WithLabels("1").Increment();
      Assert.Contains(KVP.Create(Labeled.Single("1"), 1d), labeled.Values());
      Assert.DoesNotContain(Labeled.Single("2"), labeled.Values());

      labeled.WithLabels("2").Increment(3);
      Assert.Contains(KVP.Create(Labeled.Single("1"), 1d), labeled.Values());
      Assert.Contains(KVP.Create(Labeled.Single("2"), 3d), labeled.Values());

      labeled.WithLabels("1").Decrement(3);
      Assert.Contains(KVP.Create(Labeled.Single("1"), -2d), labeled.Values());
      Assert.Contains(KVP.Create(Labeled.Single("2"), 3d), labeled.Values());
    }

    [Fact]
    public void Visit()
    {
      var noLabelInfo = noLabel.Collect();
      var labeledInfo = labeled.Collect();

      Assert.Equal("noLabel", noLabelInfo.Name);
      Assert.Equal("help noLabels", noLabelInfo.Help);
      Assert.Equal(MetricType.Gauge, noLabelInfo.Type);

      Assert.Equal("labeled", labeledInfo.Name);
      Assert.Equal("help labeled", labeledInfo.Help);
      Assert.Equal(MetricType.Gauge, labeledInfo.Type);
    }
  }
}
