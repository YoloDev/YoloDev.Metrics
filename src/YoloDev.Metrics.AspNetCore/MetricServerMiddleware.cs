using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using YoloDev.Metrics.Abstractions;
using YoloDev.Metrics.Options;
using YoloDev.Metrics.Visitors;

namespace YoloDev.Metrics.Middleware
{
  public class MetricServerMiddleware
  {
    readonly MetricServerOptions _options;
    readonly IMetricRegistry _registry;

    public MetricServerMiddleware(
      RequestDelegate next,
      IOptions<MetricServerOptions> options,
      IMetricRegistry registry)
    {
      if (options == null)
      {
        throw new ArgumentNullException(nameof(options));
      }

      if (registry == null)
      {
        throw new ArgumentNullException(nameof(registry));
      }

      _options = options.Value;
      _registry = registry;
    }

    /// <summary>
    /// Processes a request.
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
      if (httpContext == null)
      {
        throw new ArgumentNullException(nameof(httpContext));
      }

      httpContext.Response.ContentType = "text/plain; version=0.0.4; charset=utf-8";
      using (var writer = new StreamWriter(httpContext.Response.Body))
      {
        await TextOutputWriter.WriteAsync(writer, _registry, httpContext.RequestAborted);
      }
    }
  }
}
