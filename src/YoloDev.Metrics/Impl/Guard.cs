using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace YoloDev.Metrics.Impl
{
  internal static class Guard
  {
    readonly static Regex ValidMetricNameRegex = new Regex("^[a-zA-Z_:][a-zA-Z0-9_:]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    readonly static Regex ValidLabelNameRegex = new Regex("^[a-zA-Z_:][a-zA-Z0-9_:]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    readonly static Regex ReservedLabelNameRegex = new Regex("^__.*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void ArgNotNull<T>(T arg, string argName) where T : class
    {
      if (arg == null)
      {
        throw new ArgumentNullException(argName);
      }
    }

    public static void ValidMetricName(string name, string argName)
    {
      if (!ValidMetricNameRegex.IsMatch(name))
      {
        throw new ArgumentException($"Metric name '{name}' does not match regex '{ValidMetricNameRegex}'.", nameof(argName));
      }
    }

    public static ImmutableArray<string> ValidLabelNames(IEnumerable<string> labelNames)
    {
      if (labelNames == null) return ImmutableArray.Create<string>();

      var labels = new List<string>();
      foreach (var label in labelNames)
      {
        if (label == null)
        {
          throw new ArgumentNullException("label", "Label name was null");
        }

        if (!ValidLabelNameRegex.IsMatch(label))
        {
          throw new ArgumentException($"Label name '{label}' does not match regex '{ValidLabelNameRegex}'.", "label");
        }

        if (ReservedLabelNameRegex.IsMatch(label))
        {
          throw new ArgumentException($"Label name '{label}' is not valid - labels starting with double underscore are reserved!", "label");
        }

        labels.Add(label);
      }

      return labels.ToImmutableArray();
    }

    public static void InvalidLabel(string type, ImmutableArray<string> labelNames, params string[] invalidLabels)
    {
      var set = new HashSet<string>(invalidLabels, StringComparer.Ordinal);
      foreach (var label in labelNames)
      {
        if (set.Contains(label))
        {
          throw new ArgumentException($"${type} cannot have a label named '{label}'.");
        }
      }
    }
  }
}
