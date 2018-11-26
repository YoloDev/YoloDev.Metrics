// Copied from https://raw.githubusercontent.com/Netflix/ocelli/master/ocelli-core/src/main/java/netflix/ocelli/stats/CKMSQuantiles.java
// Revision d0357b8bf5c17a173ce94d6b26823775b3f999f6 from Jan 21, 2015.
//
// This is the original code except for the following modifications:
//
//  - Changed the type of the observed values from int to double.
//  - Removed the Quantiles interface and corresponding @Override annotations.
//  - Changed the package name.
//  - Make get() return NaN when no sample was observed.
//  - Make class package private
//  - Rewrite to C#

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using YoloDev.Metrics.Abstractions.Options;

namespace YoloDev.Metrics.Impl
{
  /// <summary>
  /// Implementation of the Cormode, Korn, Muthukrishnan, and Srivastava algorithm
  /// for streaming calculation of targeted high-percentile epsilon-approximate
  /// quantiles.
  /// </summary>
  /// <remarks>
  /// <para>
  /// This is a generalization of the earlier work by Greenwald and Khanna (GK),
  /// which essentially allows different error bounds on the targeted quantiles,
  /// which allows for far more efficient calculation of high-percentiles.
  /// </para>
  /// <para>
  /// See: Cormode, Korn, Muthukrishnan, and Srivastava
  /// "Effective Computation of Biased Quantiles over Data Streams" in ICDE 2005
  ///
  /// Greenwald and Khanna,
  /// "Space-efficient online computation of quantile summaries" in SIGMOD 2001
  /// </para>
  /// </remarks>
  class CKMSQuantiles
  {
    /// <summary>
    /// Lock object used for internal synchronization.
    /// </summary>
    readonly object _lock = new object();

    /// <summary>
    /// Total number of items in stream.
    /// </summary>
    int _count = 0;

    /// <summary>
    /// Current list of sampled items, maintained in sorted order with error
    /// bounds.
    /// </summary>
    readonly LinkedList<Item> _sample;

    /// <summary>
    /// Buffers incoming items to be inserted in batch.
    /// </summary>
    readonly double[] _buffer = new double[500];
    int _bufferCount = 0;

    /// <summary>
    /// Array of Quantiles that we care about, along with desired error.
    /// </summary>
    readonly ImmutableArray<Quantile> _quantiles;

    public CKMSQuantiles(ImmutableArray<Quantile> quantiles)
    {
      _quantiles = quantiles;
      _sample = new LinkedList<Item>();
    }

    /// <summary>
    /// Add a new value from the stream.
    /// </summary>
    /// <param name="value"></param>
    public void Insert(double value)
    {
      lock (_lock)
      {
        _buffer[_bufferCount] = value;
        _bufferCount++;

        if (_bufferCount == _buffer.Length)
        {
          InsertBatch();
          Compress();
        }
      }
    }

    /// <summary>
    /// Get the estimated value at the specified quantile.
    /// </summary>
    /// <param name="q">Queried quantile, e.g. 0.50 or 0.99.</param>
    /// <returns>Estimated value at that quantile.</returns>
    public double Get(double q)
    {
      lock (_lock)
      {
        // clear the buffer
        InsertBatch();
        Compress();

        if (_sample.Count == 0)
        {
          return double.NaN;
        }

        var rankMin = 0;
        var desired = (int)(q * _count);

        var first = true;
        Item prev = default;
        foreach (var cur in _sample)
        {
          if (first)
          {
            first = false;
          }
          else
          {
            rankMin += prev.G;
            if (rankMin + cur.G + cur.Delta > desired + (AllowableError(desired) / 2))
            {
              return prev.Value;
            }
          }

          prev = cur;
        }

        // edge case of wanting max value
        return prev.Value;
      }
    }

    /// <summary>
    /// Specifies the allowable error for this rank, depending on which quantiles
    /// are being targeted.
    ///
    /// This is the f(r_i, n) function from the CKMS paper. It's basically how
    /// wide the range of this rank can be.
    /// </summary>
    /// <param name="rank">the index in the list of samples</param>
    /// <returns></returns>
    double AllowableError(int rank)
    {
      // NOTE: according to CKMS, this should be count, not size, but this
      // leads
      // to error larger than the error bounds. Leaving it like this is
      // essentially a HACK, and blows up memory, but does "work".
      // var size = count;
      var size = _sample.Count;
      double minError = size + 1;

      foreach (var q in _quantiles)
      {
        double error;
        if (rank <= q.Quant * size)
        {
          error = q.U * (size - rank);
        }
        else
        {
          error = q.V * rank;
        }

        if (error < minError)
        {
          minError = error;
        }
      }

      return minError;
    }

    bool InsertBatch()
    {
      if (_bufferCount == 0)
      {
        return false;
      }

      Array.Sort(_buffer, 0, _bufferCount);

      // Base case: no samples
      int start = 0;
      if (_sample.Count == 0)
      {
        Item newItem = new Item(_buffer[0], 1, 0);
        _sample.AddLast(newItem);
        start++;
        _count++;
      }

      var it = _sample.First;
      var item = it.Value;
      var index = 0;

      for (int i = start; i < _bufferCount; i++)
      {
        double v = _buffer[i];
        while (it.Next != null && item.Value < v)
        {
          it = it.Next;
          index++;
          item = it.Value;
        }

        // If we found that bigger item, back up so we insert ourselves
        // before it
        if (item.Value > v && it.Previous != null)
        {
          it = it.Previous;
          index--;
        }

        // We use different indexes for the edge comparisons, because of the
        // above
        // if statement that adjusts the iterator
        int delta;
        if (it.Previous == null || it.Next == null)
        {
          delta = 0;
        }
        else
        {
          delta = ((int)Math.Floor(AllowableError(index + 1))) - 1;
        }

        var newItem = new Item(v, 1, delta);
        _sample.AddAfter(it, newItem);
        _count++;
        item = newItem;
      }

      _bufferCount = 0;
      return true;
    }

    /// <summary>
    /// Try to remove extraneous items from the set of sampled items. This checks
    /// if an item is unnecessary based on the desired error bounds, and merges
    /// it with the adjacent item if it is.
    /// </summary>
    void Compress()
    {
      if (_sample.Count < 2)
      {
        return;
      }

      var it = _sample.First;
      var index = 0;
      var removed = 0;

      Item prev = default;
      var next = it.Value;
      it = it.Next;
      index++;

      while (it.Next != null)
      {
        prev = next;
        next = it.Value;
        it = it.Next;
        index++;

        if (prev.G + next.G + next.Delta <= AllowableError(index - 1))
        {
          next.G += prev.G;

          // Remove prev.
          _sample.Remove(it.Previous.Previous);
          removed++;
        }
      }
    }

    class Item
    {
      public double Value { get; }
      public int Delta { get; }
      public int G { get; set; }

      public Item(double value, int lowerDelta, int delta)
      {
        Value = value;
        Delta = delta;
        G = lowerDelta;
      }
    }

    public readonly struct Quantile
    {
      public double Quant { get; }
      public double Error { get; }
      public double U { get; }
      public double V { get; }

      public Quantile(double quantile, double error)
      {
        Quant = quantile;
        Error = error;
        U = 2.0 * error / (1.0 - quantile);
        V = 2.0 * error / quantile;
      }

      internal static Quantile FromQuantileEpsilonPair(SummaryObjective pair)
        => new Quantile(pair.Quantile, pair.Epsilon);
    }
  }
}
