namespace Caching.MetricsLab;

public sealed record ThresholdResult(double Threshold, ClassificationMetrics Metrics);

public static class ThresholdEvaluator
{
    public static ClassificationMetrics Evaluate(IEnumerable<Prediction> predictions, double threshold)
    {
        ArgumentNullException.ThrowIfNull(predictions);
        if (!double.IsFinite(threshold) || threshold < 0 || threshold > 1)
            throw new ArgumentOutOfRangeException(nameof(threshold));
        int tp = 0, fp = 0, fn = 0, tn = 0;
        foreach (var row in predictions)
        {
            if (!double.IsFinite(row.Score) || row.Score < 0 || row.Score > 1)
                throw new ArgumentException("Scores must be finite and between 0 and 1.", nameof(predictions));
            if (row.Score >= threshold)
            {
                if (row.Actual) tp++; else fp++;
            }
            else if (row.Actual) fn++; else tn++;
        }
        return new(tp, fp, fn, tn);
    }

    public static IReadOnlyList<ThresholdResult> Sweep(IEnumerable<Prediction> predictions, IEnumerable<double> thresholds)
    {
        ArgumentNullException.ThrowIfNull(predictions);
        ArgumentNullException.ThrowIfNull(thresholds);
        var rows = predictions.ToArray();
        return thresholds.Distinct().Order().Select(t => new ThresholdResult(t, Evaluate(rows, t))).ToArray();
    }
}
