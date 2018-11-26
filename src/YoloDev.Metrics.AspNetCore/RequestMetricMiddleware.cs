using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using YoloDev.Metrics.Options;

namespace YoloDev.Metrics.Middleware
{
  public class RequestMetricMiddleware
  {
    const string DurationHelpText = "duration histogram of http responses labeled with: ";

    readonly ISummary _requestDurations;
    readonly RequestDelegate _next;
    readonly RequestMetricOptions _options;
    readonly int _labelsCount;

    public RequestMetricMiddleware(
      RequestDelegate next,
      IOptions<RequestMetricOptions> options,
      IMetricFactory<RequestMetricMiddleware> factory)
    {
      _next = next;
      _options = options.Value;

      var labels = new List<string>();

      if (_options.IncludeStatusCode)
      {
        labels.Add("status_code");
      }

      if (_options.IncludeMethod)
      {
        labels.Add("method");
      }

      if (_options.IncludePath)
      {
        labels.Add("path");
      }

      if (_options.CustomLabels != null)
      {
        foreach (var customLabel in _options.CustomLabels)
        {
          labels.Add(customLabel.Key);
        }
      }

      var helpTextEnd = string.Join(", ", labels);
      _labelsCount = labels.Count;
      _requestDurations = factory.CreateSummary(_options.RequestDurationMetricName, opts =>
      {
        opts.Help = DurationHelpText + helpTextEnd;
        opts.LabelNames = labels;
        if (_options.RequestDurationObjectives != null)
        {
          opts.Objectives = _options.RequestDurationObjectives;
        }
      });
    }

    public async Task Invoke(HttpContext context)
    {
      // TODO: Use pathstrings instead?
      string route = context.Request.Path.ToString().ToLower();

      if (_options.IgnoreRoutesStartWith != null && _options.IgnoreRoutesStartWith.Any(i => route.StartsWith(i)))
      {
        await _next.Invoke(context);
        return;
      }

      if (_options.IgnoreRoutesContains != null && _options.IgnoreRoutesContains.Any(i => route.Contains(i)))
      {
        await _next.Invoke(context);
        return;
      }

      if (_options.IgnoreRoutesConcrete != null && _options.IgnoreRoutesConcrete.Any(i => route == i))
      {
        await _next.Invoke(context);
        return;
      }

      var watch = Stopwatch.StartNew();
      await _next.Invoke(context);
      watch.Stop();

      var labelValues = new string[_labelsCount];
      var index = 0;
      if (_options.IncludeStatusCode)
      {
        labelValues[index++] = context.Response.StatusCode.ToString();
      }

      if (_options.IncludeMethod)
      {
        labelValues[index++] = context.Request.Method;
      }

      if (_options.IncludePath)
      {
        labelValues[index++] = route;
      }

      if (_options.CustomLabels != null)
      {
        foreach (var customLabel in _options.CustomLabels)
        {
          labelValues[index++] = customLabel.Value;
        }
      }

      _requestDurations.WithLabels(labelValues).Observe(watch.Elapsed.TotalSeconds);
    }
  }
}
