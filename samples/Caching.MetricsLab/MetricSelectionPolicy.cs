namespace Caching.MetricsLab;

public enum SelectionGoal { BalancedF1, RecallWithPrecisionFloor, PrecisionWithRecallFloor }

public static class MetricSelectionPolicy
{
    // Ties favor fewer prefetches, then a higher threshold. No feasible threshold returns null.
    public static ThresholdResult? Select(IEnumerable<ThresholdResult> candidates, SelectionGoal goal,
        double minimum = 0, int? maxPrefetch = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!Enum.IsDefined(goal)) throw new ArgumentOutOfRangeException(nameof(goal));
        if (!double.IsFinite(minimum) || minimum < 0 || minimum > 1)
            throw new ArgumentOutOfRangeException(nameof(minimum));
        if (maxPrefetch is < 0) throw new ArgumentOutOfRangeException(nameof(maxPrefetch));
        return candidates.Where(r => r.Metrics.Count > 0 &&
                (!maxPrefetch.HasValue || r.Metrics.Prefetched <= maxPrefetch.Value))
            .Where(r => goal switch
            {
                SelectionGoal.RecallWithPrecisionFloor => r.Metrics.Prefetched > 0 && r.Metrics.Precision >= minimum,
                SelectionGoal.PrecisionWithRecallFloor => r.Metrics.TP + r.Metrics.FN > 0 && r.Metrics.Recall >= minimum,
                _ => true
            })
            .OrderByDescending(r => goal switch
            {
                SelectionGoal.RecallWithPrecisionFloor => r.Metrics.Recall,
                SelectionGoal.PrecisionWithRecallFloor => r.Metrics.Precision,
                _ => r.Metrics.F1
            })
            .ThenBy(r => r.Metrics.Prefetched).ThenByDescending(r => r.Threshold).FirstOrDefault();
    }
}
