using Caching.MetricsLab;

namespace Caching.MetricsLab.Tests;

public class ThresholdEvaluatorTests
{
    private static readonly ThresholdResult[] Candidates =
    [new(.3, new(5, 5, 0, 10)), new(.5, new(4, 1, 1, 14)), new(.7, new(2, 0, 3, 15))];

    [Fact]
    public void ChoosesMetricAccordingToBusinessConstraints()
    {
        Assert.Equal(.5, MetricSelectionPolicy.Select(Candidates, SelectionGoal.BalancedF1)!.Threshold);
        Assert.Equal(.5, MetricSelectionPolicy.Select(Candidates, SelectionGoal.RecallWithPrecisionFloor, .8, 5)!.Threshold);
        Assert.Equal(.7, MetricSelectionPolicy.Select(Candidates, SelectionGoal.RecallWithPrecisionFloor, .8, 2)!.Threshold);
        Assert.Equal(.5, MetricSelectionPolicy.Select(Candidates, SelectionGoal.PrecisionWithRecallFloor, .8)!.Threshold);
        Assert.Null(MetricSelectionPolicy.Select(Candidates, SelectionGoal.PrecisionWithRecallFloor, 1, 5));
    }

    [Fact]
    public void TiesFavorFewerPrefetchesThenHigherThreshold()
    {
        ThresholdResult[] tied = [new(.4, new(1, 0, 1, 2)), new(.8, new(1, 0, 1, 2))];
        Assert.Equal(.8, MetricSelectionPolicy.Select(tied, SelectionGoal.BalancedF1)!.Threshold);
        Assert.Null(MetricSelectionPolicy.Select([], SelectionGoal.BalancedF1));
        Assert.Null(MetricSelectionPolicy.Select([new(.5, new(0, 0, 0, 0))], SelectionGoal.BalancedF1));
    }

    [Fact]
    public void SweepSortsDeduplicatesAndIncludesEqualityAtThreshold()
    {
        var results = ThresholdEvaluator.Sweep([new("k", true, .5)], [.7, .5, .5]);
        Assert.Equal(2, results.Count);
        Assert.Equal(1, results[0].Metrics.TP); Assert.Equal(1, results[1].Metrics.FN);
    }

    [Fact]
    public void RejectsInvalidPolicyConstraints()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MetricSelectionPolicy.Select(Candidates, SelectionGoal.BalancedF1, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => MetricSelectionPolicy.Select(Candidates, SelectionGoal.BalancedF1, maxPrefetch: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => MetricSelectionPolicy.Select(Candidates, (SelectionGoal)99));
    }
}
