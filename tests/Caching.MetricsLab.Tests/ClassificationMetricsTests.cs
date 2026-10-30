using Caching.MetricsLab;

namespace Caching.MetricsLab.Tests;

public class ClassificationMetricsTests
{
    [Fact]
    public void HandCalculatedConfusionMatrixAndMetrics()
    {
        Prediction[] rows = [new("a", true, .9), new("b", true, .5), new("c", false, .8),
            new("d", true, .1), new("e", false, .2)];
        var m = ThresholdEvaluator.Evaluate(rows, .5);
        Assert.Equal(new ClassificationMetrics(2, 1, 1, 1), m);
        Assert.Equal(2.0 / 3, m.Precision, 10);
        Assert.Equal(2.0 / 3, m.Recall, 10);
        Assert.Equal(2.0 / 3, m.F1, 10);
        Assert.Equal(.6, m.Accuracy, 10);
    }

    [Fact]
    public void NeverPrefetchBaselineExposesMisleadingAccuracy()
    {
        var rows = Enumerable.Range(0, 100).Select(i => new Prediction($"k{i}", i < 5, 0));
        var m = ThresholdEvaluator.Evaluate(rows, .5);
        Assert.Equal(.95, m.Accuracy);
        Assert.Equal(0, m.Precision); Assert.Equal(0, m.Recall); Assert.Equal(0, m.F1);
        Assert.Equal(0, ThresholdEvaluator.Evaluate([], .5).F1);
    }

    [Theory]
    [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(-.1)] [InlineData(1.1)]
    public void RejectsInvalidThresholdAndScore(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ThresholdEvaluator.Evaluate([], value));
        Assert.Throws<ArgumentException>(() => ThresholdEvaluator.Evaluate([new("k", true, value)], .5));
    }

    [Fact]
    public void NoActualPositivesHasZeroRecall()
    {
        var m = ThresholdEvaluator.Evaluate([new("k", false, 1)], 1);
        Assert.Equal(new ClassificationMetrics(0, 1, 0, 0), m);
        Assert.Equal(0, m.Recall); Assert.Equal(0, m.F1);
    }
}
