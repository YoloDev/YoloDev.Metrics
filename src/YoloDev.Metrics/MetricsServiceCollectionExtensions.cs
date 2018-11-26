using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using YoloDev.Metrics;
using YoloDev.Metrics.Abstractions;
using YoloDev.Metrics.Impl;

namespace Microsoft.Extensions.DependencyInjection
{
  public static class MetricsServiceCollectionExtensions
  {
    public static IServiceCollection AddMetrics(this IServiceCollection collection)
    {
      collection.TryAddSingleton<IClock>(SystemClock.Instance);
      collection.TryAddSingleton<IMetricRegistry, DefaultMetricRegistry>();
      collection.TryAddSingleton<IMetricFactory, DefaultMetricFactory>();
      collection.TryAddSingleton(typeof(IMetricFactory<>), typeof(DefaultMetricFactory<>));

      return collection;
    }
  }
}
