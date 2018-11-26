using System.Collections.Generic;

namespace YoloDev.Metrics.Impl
{
  static class Extensions
  {
    internal static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
    {
      key = kvp.Key;
      value = kvp.Value;
    }

    internal static void Increment(this AtomicDouble adder, double value) =>
      adder.Modify((n, m) => n + m, value);

    internal static void Decrement(this AtomicDouble adder, double value) =>
      adder.Modify((n, m) => n - m, value);

    internal static void Increment(this AtomicLong adder, long value) =>
      adder.Modify((n, m) => n + m, value);

  }
}
