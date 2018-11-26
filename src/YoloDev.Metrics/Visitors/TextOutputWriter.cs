using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YoloDev.Metrics.Abstractions;
using YoloDev.Metrics.Impl;

namespace YoloDev.Metrics.Visitors
{
  public class TextOutputWriter : IMetricVisitor<Task>
  {
    readonly TextWriter _writer;
    readonly CancellationToken _token;
    string _name;
    MetricType _type;

    public static async Task WriteAsync(TextWriter writer, IMetricRegistry registry, CancellationToken token = default)
    {
      var visitor = new TextOutputWriter(writer, token);
      await registry.Visit(visitor);
    }

    private TextOutputWriter(TextWriter writer, CancellationToken token)
    {
      Guard.ArgNotNull(writer, nameof(writer));

      _writer = writer;
      _token = token;
    }

    async Task IMetricVisitor<Task>.VisitCollection(IEnumerable<IMetricFamily> metrics)
    {
      foreach (var metric in metrics)
      {
        await metric.Visit(this);
        _token.ThrowIfCancellationRequested();
      }
    }

    async Task IMetricVisitor<Task>.VisitFamily(string name, string help, MetricType type, IEnumerable<IMetric> metrics)
    {
      using (var e = metrics.GetEnumerator())
      {
        if (!e.MoveNext()) return;

        Debug.Assert(_name == null);
        _name = name;
        _type = type;
        if (!string.IsNullOrEmpty(help))
        {
          // # HELP familyname helptext
          await _writer.WriteAsync("# HELP ");
          await _writer.WriteAsync(name);
          await _writer.WriteAsync(" ");
          await _writer.WriteAsync(help);
          await _writer.WriteAsync("\n");
        }

        // # TYPE familyname type
        await _writer.WriteAsync("# TYPE ");
        await _writer.WriteAsync(name);
        await _writer.WriteAsync(" ");
        await _writer.WriteAsync(TypeName(type));
        await _writer.WriteAsync("\n");

        do
        {
          await e.Current.Visit(this);
          _token.ThrowIfCancellationRequested();
        } while (e.MoveNext());

        await _writer.WriteAsync("\n");
        _name = null;
      }
    }

    async Task IMetricVisitor<Task>.VisitCounter(double value, IEnumerable<(string name, string value)> labels)
    {
      Debug.Assert(_type == MetricType.Counter);
      await WriteMetricWithLabels(_writer, _name, null, value, labels);
    }

    async Task IMetricVisitor<Task>.VisitGauge(double value, IEnumerable<(string name, string value)> labels)
    {
      Debug.Assert(_type == MetricType.Gauge);
      await WriteMetricWithLabels(_writer, _name, null, value, labels);
    }

    async Task IMetricVisitor<Task>.VisitHistogram(long sampleCount, double sampleSum, IEnumerable<(long cumulativeCount, double upperBound)> buckets, IEnumerable<(string name, string value)> labels)
    {
      Debug.Assert(_type == MetricType.Histogram);
      await WriteMetricWithLabels(_writer, _name, "_sum", sampleSum, labels);
      await WriteMetricWithLabels(_writer, _name, "_count", sampleCount, labels);

      foreach (var bucket in buckets)
      {
        var value = double.IsPositiveInfinity(bucket.upperBound) ?
          "+Inf" : bucket.upperBound.ToString(CultureInfo.InvariantCulture);

        var bucketLabels = labels.Concat(new[] { ("le", value) });
        await WriteMetricWithLabels(_writer, _name, "_bucket", bucket.cumulativeCount, bucketLabels);
      }
    }

    async Task IMetricVisitor<Task>.VisitSummary(long sampleCount, double sampleSum, IEnumerable<(double quantile, double value)> samples, IEnumerable<(string name, string value)> labels)
    {
      Debug.Assert(_type == MetricType.Summary);
      await WriteMetricWithLabels(_writer, _name, "_sum", sampleSum, labels);
      await WriteMetricWithLabels(_writer, _name, "_count", sampleCount, labels);

      foreach (var sample in samples)
      {
        var quantile = double.IsPositiveInfinity(sample.quantile) ?
          "+Inf" : sample.quantile.ToString(CultureInfo.InvariantCulture);

        var quantlieLabels = labels.Concat(new[] { ("quantile", quantile) });
        await WriteMetricWithLabels(_writer, _name, null, sample.value, quantlieLabels);
      }
    }

    static async Task WriteMetricWithLabels(TextWriter writer, string familyName, string postfix, double value, IEnumerable<(string name, string value)> labels)
    {
      // familyname_postfix{labelkey1="labelvalue1",labelkey2="labelvalue2"} value
      await writer.WriteAsync(familyName);

      if (postfix != null)
      {
        await writer.WriteAsync(postfix);
      }

      using (var e = labels.GetEnumerator())
      {
        if (e.MoveNext())
        {
          // we have labels
          await writer.WriteAsync("{");
          bool firstLabel = true;

          do
          {
            if (!firstLabel)
            {
              await writer.WriteAsync(",");
            }

            firstLabel = false;
            var (labelName, labelValue) = e.Current;
            await writer.WriteAsync(labelName);
            await writer.WriteAsync("=\"");
            await writer.WriteAsync(EscapeLabelValue(labelValue));
            await writer.WriteAsync('"');
          } while (e.MoveNext());

          await writer.WriteAsync("}");
        }
      }

      await writer.WriteAsync(" ");
      await writer.WriteAsync(value.ToString(CultureInfo.InvariantCulture));
      await writer.WriteAsync("\n");
    }

    static string EscapeLabelValue(string value)
    {
      var sb = new StringBuilder(value);
      sb.Replace("\\", @"\\");
      sb.Replace("\n", @"\n");
      sb.Replace("\"", @"\""");
      return sb.ToString();
    }

    static string TypeName(MetricType type)
    {
      switch (type)
      {
        case MetricType.Counter: return "counter";
        case MetricType.Gauge: return "gauge";
        case MetricType.Histogram: return "histogram";
        case MetricType.Summary: return "summary";
      }

      throw new ArgumentOutOfRangeException(nameof(type));
    }
  }
}
