using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using YoloDev.Metrics.Abstractions.Options;
using YoloDev.Metrics.Histogram;
using YoloDev.Metrics.Impl;

namespace YoloDev.Metrics.Tests
{
  public class HistogramTests
  {
    readonly IHistogram noLabel;
    readonly IHistogram labeled;

    public HistogramTests()
    {
      noLabel = new DefaultHistogram("noLabel", new HistogramOptions
      {
        Help = "help noLabels",
      });

      labeled = new DefaultHistogram("labeled", new HistogramOptions
      {
        Help = "help labeled",
        LabelNames = Labeled.SingleLabels
      });
    }

    [Fact]
    public void Observe()
    {
      noLabel.Observe(2);
      Assert.Equal(1, noLabel.Count());
      Assert.Equal(2, noLabel.Sum());
      Assert.Equal(0, noLabel.BucketValue(1));
      Assert.Equal(1, noLabel.BucketValue(2.5));

      noLabel.WithLabels().Observe(4);
      Assert.Equal(2, noLabel.Count());
      Assert.Equal(6, noLabel.Sum());
      Assert.Equal(0, noLabel.BucketValue(1));
      Assert.Equal(1, noLabel.BucketValue(2.5));
      Assert.Equal(2, noLabel.BucketValue(5));
      Assert.Equal(2, noLabel.BucketValue(7.5));
      Assert.Equal(2, noLabel.BucketValue(10));
      Assert.Equal(2, noLabel.BucketValue(double.PositiveInfinity));
    }

    [Fact]
    public void BoundaryConditions()
    {
      // Equal to a bucket.
      noLabel.Observe(2.5);
      Assert.Equal(0, noLabel.BucketValue(1));
      Assert.Equal(1, noLabel.BucketValue(2.5));

      // Infinity.
      noLabel.WithLabels().Observe(double.PositiveInfinity);
      Assert.Equal(0, noLabel.BucketValue(1));
      Assert.Equal(1, noLabel.BucketValue(2.5));
      Assert.Equal(1, noLabel.BucketValue(5));
      Assert.Equal(1, noLabel.BucketValue(7.5));
      Assert.Equal(1, noLabel.BucketValue(10));
      Assert.Equal(2, noLabel.BucketValue(double.PositiveInfinity));
    }

    [Fact]
    public void CustomBuckets()
    {
      var h = new DefaultHistogram("h", new HistogramOptions
      {
        Buckets = new double[] { 1, 2 }
      });

      Assert.Equal(
        new double[] { 1, 2, double.PositiveInfinity },
        h.Value().Buckets.Select(b => b.UpperBound));
    }

    [Fact]
    public void CustomBucketsWithInfinity()
    {
      var h = new DefaultHistogram("h", new HistogramOptions
      {
        Buckets = new double[] { 1, 2, double.PositiveInfinity }
      });

      Assert.Equal(
        new double[] { 1, 2, double.PositiveInfinity },
        h.Value().Buckets.Select(b => b.UpperBound));
    }

    [Fact]
    public void LinearBuckets()
    {
      var buckets = Buckets.Linear(1, 2, 3);
      Assert.Equal(
        new double[] { 1, 3, 5 },
        buckets
      );
    }

    [Fact]
    public void ExponentialBuckets()
    {
      var buckets = Buckets.Exponential(2, 2.5, 3);
      Assert.Equal(
        new double[] { 2, 5, 12.5 },
        buckets
      );
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
    public void LabeledValues()
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
    public void LeLabelThrows()
    {
      Assert.Throws<ArgumentException>(() => new DefaultHistogram("h", new HistogramOptions
      {
        LabelNames = new[] { "le" }
      }));

      Assert.Throws<ArgumentException>(() => new DefaultHistogram("h", new HistogramOptions
      {
        LabelNames = new[] { "foo", "bar", "le" }
      }));
    }

    [Fact]
    public void Visit()
    {
      var noLabelInfo = noLabel.Collect();
      var labeledInfo = labeled.Collect();

      Assert.Equal("noLabel", noLabelInfo.Name);
      Assert.Equal("help noLabels", noLabelInfo.Help);
      Assert.Equal(MetricType.Histogram, noLabelInfo.Type);

      Assert.Equal("labeled", labeledInfo.Name);
      Assert.Equal("help labeled", labeledInfo.Help);
      Assert.Equal(MetricType.Histogram, labeledInfo.Type);
    }
  }
}
