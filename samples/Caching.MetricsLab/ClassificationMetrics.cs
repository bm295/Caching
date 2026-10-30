namespace Caching.MetricsLab;

public sealed record Prediction(string Key, bool Actual, double Score);

public sealed record ClassificationMetrics(int TP, int FP, int FN, int TN)
{
    public int Count => TP + FP + FN + TN;
    public int Prefetched => TP + FP;
    // Undefined ratios are reported as zero throughout this teaching lab.
    public double Precision => Divide(TP, TP + FP);
    public double Recall => Divide(TP, TP + FN);
    public double F1 => Divide(2.0 * TP, 2.0 * TP + FP + FN);
    public double Accuracy => Divide(TP + TN, Count);
    private static double Divide(double numerator, double denominator) => denominator == 0 ? 0 : numerator / denominator;
}
