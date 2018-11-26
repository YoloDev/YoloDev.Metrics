using System;
using System.Threading;

namespace YoloDev.Metrics.Impl
{
  internal class AtomicDouble
  {
    long _value;

    public AtomicDouble(double value)
    {
      _value = BitConverter.DoubleToInt64Bits(value);
    }

    public double Value
      => BitConverter.Int64BitsToDouble(Interlocked.Read(ref _value));

    public void SetValue(double value)
    {
      Interlocked.Exchange(ref _value, BitConverter.DoubleToInt64Bits(value));
    }

    public double Modify<TArg>(Func<double, TArg, double> func, TArg arg)
    {
      while (true)
      {
        long initialValue = _value;
        double computedValue = func(BitConverter.Int64BitsToDouble(initialValue), arg);

        //Compare exchange will only set the computed value if it is equal to the expected value
        //It will always return the the value of _value prior to the exchange (whether it happens or not)
        //So, only exit the loop if the value was what we expected it to be (initialValue) at the time of exchange otherwise another thread updated and we need to try again.
        if (initialValue == Interlocked.CompareExchange(ref _value, BitConverter.DoubleToInt64Bits(computedValue), initialValue))
          return computedValue;
      }
    }
  }
}
