using System;
using System.Collections.Generic;

namespace YoloDev.Metrics.Histogram
{
  public static class Buckets
  {
    public static IEnumerable<double> Linear(double start, double width, int count)
    {
      for (int i = 0; i < count; i++)
      {
        yield return start + i * width;
      }
    }

    public static IEnumerable<double> Exponential(double start, double factor, int count)
    {
      for (int i = 0; i < count; i++)
      {
        yield return start * Math.Pow(factor, i);
      }
    }
  }
}
