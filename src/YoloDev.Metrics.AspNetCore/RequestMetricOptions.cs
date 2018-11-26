using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using YoloDev.Metrics.Abstractions.Options;

namespace YoloDev.Metrics.Options
{
  public class RequestMetricOptions
  {
    /// <summary>
    ///     Metric name
    /// </summary>
    public string RequestDurationMetricName { get; set; } = "http_request_duration_seconds";

    /// <summary>
    ///     HTTP status code (200, 400, 404 etc.)
    /// </summary>
    public bool IncludeStatusCode { get; set; } = true;

    /// <summary>
    ///     HTTP method (GET, PUT, ...)
    /// </summary>
    public bool IncludeMethod { get; set; } = false;

    /// <summary>
    ///     URL path
    /// </summary>
    public bool IncludePath { get; set; } = false;

    /// <summary>
    ///     Ignore paths
    /// </summary>
    public string[] IgnoreRoutesConcrete { get; set; }

    /// <summary>
    ///     Ignore paths
    /// </summary>
    public string[] IgnoreRoutesContains { get; set; }

    /// <summary>
    ///     Ignore paths
    /// </summary>
    public string[] IgnoreRoutesStartWith { get; set; }

    /// <summary>
    ///     Custom Labels
    /// </summary>
    public Dictionary<string, string> CustomLabels { get; set; }

    public IEnumerable<SummaryObjective> RequestDurationObjectives { get; set; }
  }
}
