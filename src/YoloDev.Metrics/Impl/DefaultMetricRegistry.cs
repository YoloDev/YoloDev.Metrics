using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using YoloDev.Metrics.Abstractions;

namespace YoloDev.Metrics.Impl
{
  public class DefaultMetricRegistry : IMetricRegistry
  {
    ImmutableSortedDictionary<string, IMetricFamily> _metrics = ImmutableSortedDictionary.Create<string, IMetricFamily>();

    public IMetric GetOrAdd<TArg>(string name, Func<string, TArg, IMetricFamily> factory, TArg factoryArg)
    {
      var metric = GetOrAdd(ref _metrics, name, factory, factoryArg);
      Debug.Assert(metric.Name == name);
      return metric;
    }

    public bool Remove(IMetricFamily metric) =>
      TryRemove(ref _metrics, metric.Name, out IMetricFamily _);


    public T Visit<T>(IMetricVisitor<T> visitor)
      => visitor.VisitCollection(_metrics.Values);

    // Following methods copied from https://github.com/dotnet/corefx/blob/master/src/System.Collections.Immutable/src/System/Collections/Immutable/ImmutableInterlocked.cs
    static TValue GetOrAdd<TKey, TValue, TArg>(ref ImmutableSortedDictionary<TKey, TValue> location, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
    {
      Guard.ArgNotNull(valueFactory, nameof(valueFactory));

      var map = Volatile.Read(ref location);
      Guard.ArgNotNull(map, nameof(location));

      TValue value;
      if (map.TryGetValue(key, out value))
      {
        return value;
      }

      value = valueFactory(key, factoryArgument);
      return GetOrAdd(ref location, key, value);
    }

    static TValue GetOrAdd<TKey, TValue>(ref ImmutableSortedDictionary<TKey, TValue> location, TKey key, TValue value)
    {
      var priorCollection = Volatile.Read(ref location);
      bool successful;
      do
      {
        Guard.ArgNotNull(priorCollection, nameof(location));
        TValue oldValue;
        if (priorCollection.TryGetValue(key, out oldValue))
        {
          return oldValue;
        }

        var updatedCollection = priorCollection.Add(key, value);
        var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
        successful = object.ReferenceEquals(priorCollection, interlockedResult);
        priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
      }
      while (!successful);

      // We won the race-condition and have updated the collection.
      // Return the value that is in the collection (as of the Interlocked operation).
      return value;
    }

    static bool TryRemove<TKey, TValue>(ref ImmutableSortedDictionary<TKey, TValue> location, TKey key, out TValue value)
    {
      var priorCollection = Volatile.Read(ref location);
      bool successful;
      do
      {
        Guard.ArgNotNull(priorCollection, nameof(location));

        if (!priorCollection.TryGetValue(key, out value))
        {
          return false;
        }

        var updatedCollection = priorCollection.Remove(key);
        var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
        successful = object.ReferenceEquals(priorCollection, interlockedResult);
        priorCollection = interlockedResult; // we already have a volatile read that we can reuse for the next loop
      } while (!successful);

      return true;
    }
  }
}
