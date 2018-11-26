using System.Collections.Immutable;
using NodaTime;
using static YoloDev.Metrics.Impl.CKMSQuantiles;

namespace YoloDev.Metrics.Impl
{
  /// <summary>
  /// Wrapper around CKMSQuantiles.
  ///
  /// Maintains a ring buffer of CKMSQuantiles to provide quantiles over a sliding windows of time.
  /// </summary>
  class TimeWindowQuantiles
  {
    readonly object _lock = new object();
    readonly IClock _clock;
    readonly ImmutableArray<Quantile> _quantiles;
    readonly CKMSQuantiles[] _ringBuffer;
    int _currentBucket;
    Instant _lastRotateTimestamp;
    Duration _durationBetweenRotates;

    public TimeWindowQuantiles(IClock clock, ImmutableArray<Quantile> quantiles, Duration maxAge, int ageBuckets)
    {
      _clock = clock;
      _quantiles = quantiles;
      _ringBuffer = new CKMSQuantiles[ageBuckets];
      for (int i = 0; i < ageBuckets; i++)
      {
        _ringBuffer[i] = new CKMSQuantiles(quantiles);
      }

      _currentBucket = 0;
      _lastRotateTimestamp = clock.GetCurrentInstant();
      _durationBetweenRotates = maxAge / ageBuckets;
    }

    public double this[double q] => Rotate().Get(q);

    public void Insert(double value)
    {
      Rotate();
      foreach (var ckmsQuantiles in _ringBuffer)
      {
        ckmsQuantiles.Insert(value);
      }
    }

    CKMSQuantiles Rotate()
    {
      var currentTime = _clock.GetCurrentInstant();
      lock (_lock)
      {
        var timeSinceLastRotate = currentTime - _lastRotateTimestamp;
        while (timeSinceLastRotate > _durationBetweenRotates)
        {
          _ringBuffer[_currentBucket] = new CKMSQuantiles(_quantiles);
          if (++_currentBucket >= _ringBuffer.Length)
          {
            _currentBucket = 0;
          }

          timeSinceLastRotate -= _durationBetweenRotates;
          _lastRotateTimestamp += _durationBetweenRotates;
        }

        return _ringBuffer[_currentBucket];
      }
    }
  }
}
