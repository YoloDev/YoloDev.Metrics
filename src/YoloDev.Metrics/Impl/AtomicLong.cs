using System;
using System.Threading;

namespace YoloDev.Metrics.Impl
{
  internal class AtomicLong
  {
    long _value;

    public AtomicLong(long value)
    {
      _value = value;
    }

    public long Value => Interlocked.Read(ref _value);

    public long Modify<TArg>(Func<long, TArg, long> func, TArg arg)
    {
      while (true)
      {
        long initialValue = _value;
        long computedValue = func(initialValue, arg);

        //Compare exchange will only set the computed value if it is equal to the expected value
        //It will always return the the value of _value prior to the exchange (whether it happens or not)
        //So, only exit the loop if the value was what we expected it to be (initialValue) at the time of exchange otherwise another thread updated and we need to try again.
        if (initialValue == Interlocked.CompareExchange(ref _value, computedValue, initialValue))
          return computedValue;
      }
    }
  }
}
