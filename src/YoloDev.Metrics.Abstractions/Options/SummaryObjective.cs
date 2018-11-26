namespace YoloDev.Metrics.Abstractions.Options
{
  public readonly struct SummaryObjective
  {
    public SummaryObjective(double quantile, double epsilon)
    {
      Quantile = quantile;
      Epsilon = epsilon;
    }

    public double Quantile { get; }
    public double Epsilon { get; }
  }
}
