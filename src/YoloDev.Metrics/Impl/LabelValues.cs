using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace YoloDev.Metrics.Impl
{
  public struct LabelValues : IEquatable<LabelValues>, IEnumerable<(string name, string value)>
  {
    readonly ImmutableArray<string> _names;
    readonly ImmutableArray<string> _values;
    readonly int _hashCode;

    public LabelValues(ImmutableArray<string> names, ImmutableArray<string> values)
    {
      _names = names;
      _values = values;
      _hashCode = CalculateHashCode(names, values);
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<(string name, string value)> IEnumerable<(string name, string value)>.GetEnumerator()
      => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
      => GetEnumerator();

    public bool Equals(LabelValues other)
    {
      if (_hashCode != other._hashCode) return false;
      if (other._values.Length != _values.Length) return false;

      // we ignore the names, cause they should never be different in this check
      for (int i = 0; i < _values.Length; i++)
      {
        if (!string.Equals(_values[i], other._values[i], StringComparison.Ordinal))
          return false;
      }

      return true;
    }

    public override int GetHashCode() => _hashCode;
    public override bool Equals(object obj)
    {
      if (obj is LabelValues l) return Equals(l);
      return false;
    }

    static int CalculateHashCode(ImmutableArray<string> names, ImmutableArray<string> values)
    {
      unchecked
      {
        int hashCode = 0;

        for (int i = 0; i < names.Length; i++)
        {
          hashCode ^= (names[i].GetHashCode() * 397);
        }

        for (int i = 0; i < values.Length; i++)
        {
          hashCode ^= (values[i].GetHashCode() * 397);
        }

        return hashCode;
      }
    }

    public struct Enumerator : IEnumerator<(string name, string value)>
    {
      readonly LabelValues _values;
      int _index;
      bool _complete;

      public Enumerator(LabelValues values)
      {
        _values = values;
        _index = -1;
        _complete = false;
      }

      public bool MoveNext()
      {
        if (_complete)
        {
          _index = _values._values.Length;
          return false;
        }

        _index++;
        if (_index >= _values._values.Length)
        {
          _complete = true;
        }

        return !_complete;
      }

      public (string name, string value) Current
      {
        get
        {
          if (_index < 0) throw new InvalidOperationException("Enumerator not started");
          if (_complete) throw new InvalidOperationException("Enumerator ended");
          return (_values._names[_index], _values._values[_index]);
        }
      }

      object IEnumerator.Current => Current;

      public void Reset()
      {
        _index = -1;
        _complete = false;
      }

      void IDisposable.Dispose() { }
    }
  }
}
