using System.Collections.Generic;

namespace YoloDev.Metrics.Tests
{
  internal static class KVP
  {
    public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
      => new KeyValuePair<TKey, TValue>(key, value);
  }
}
