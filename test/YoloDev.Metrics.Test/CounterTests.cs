using System;
using System.Collections.Generic;
using Xunit;
using YoloDev.Metrics.Abstractions.Options;
using YoloDev.Metrics.Impl;

namespace YoloDev.Metrics.Test
{
  public class CounterTests
  {
    readonly ICounter noLabel;
    readonly ICounter labeled;

    public CounterTests()
    {
      noLabel = new DefaultCounter("noLabel", new CounterOptions
      {
        Help = "help noLabels",
      });

      labeled = new DefaultCounter("labeled", new CounterOptions
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
    public void NegativeIncrementFails()
    {
      Assert.Throws<ArgumentOutOfRangeException>(() => noLabel.Increment(-1));
      Assert.Throws<ArgumentOutOfRangeException>(() => labeled.WithLabels("1").Increment(-1));
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
    }

    [Fact]
    public void Visit()
    {
      var noLabelInfo = noLabel.Collect();
      var labeledInfo = labeled.Collect();

      Assert.Equal("noLabel", noLabelInfo.Name);
      Assert.Equal("help noLabels", noLabelInfo.Help);
      Assert.Equal(MetricType.Counter, noLabelInfo.Type);

      Assert.Equal("labeled", labeledInfo.Name);
      Assert.Equal("help labeled", labeledInfo.Help);
      Assert.Equal(MetricType.Counter, labeledInfo.Type);
    }
  }
}
