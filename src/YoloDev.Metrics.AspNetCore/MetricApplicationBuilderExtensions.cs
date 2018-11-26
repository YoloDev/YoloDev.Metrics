using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using YoloDev.Metrics.Middleware;
using YoloDev.Metrics.Options;

namespace Microsoft.AspNetCore.Builder
{
  public static class MetricApplicationBuilderExtensions
  {
    public static IApplicationBuilder UseMetricServer(this IApplicationBuilder app)
      => app.UseMetricServer("/metrics");

    public static IApplicationBuilder UseMetricServer(this IApplicationBuilder appl, PathString path)
      => appl.UseMetricServerCore(path, new MetricServerOptions());

    static IApplicationBuilder UseMetricServerCore(this IApplicationBuilder app, PathString path, MetricServerOptions options)
    {
      app.Map(path, b => b.UseMiddleware<MetricServerMiddleware>(Options.Create(options)));

      return app;
    }

    public static IApplicationBuilder UseRequestMetric(this IApplicationBuilder app)
      => app.UseRequestMetric(_ => { });

    public static IApplicationBuilder UseRequestMetric(this IApplicationBuilder app, Action<RequestMetricOptions> init)
      => app.UseRequestMetricCore(init);

    static IApplicationBuilder UseRequestMetricCore(this IApplicationBuilder app, Action<RequestMetricOptions> init)
    {
      var options = new RequestMetricOptions();
      init(options);
      app.UseMiddleware<RequestMetricMiddleware>(Options.Create(options));

      return app;
    }
  }
}
