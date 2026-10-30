using System.Globalization;
using Caching.MetricsLab;

var dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
var validation = Load("validation.csv");
// Fixed teaching grid, including 0 and 1. Prediction uses score >= threshold.
var sweep = ThresholdEvaluator.Sweep(validation, Enumerable.Range(0, 11).Select(i => i / 10.0));
Console.WriteLine("VALIDATION: threshold TP FP FN TN prefetch precision recall F1 accuracy");
foreach (var r in sweep) Print(r);
Console.WriteLine("\nValidation precision-recall points (recall, precision):");
foreach (var r in sweep)
    Console.WriteLine(FormattableString.Invariant($"t={r.Threshold:F1}: ({r.Metrics.Recall:F3}, {r.Metrics.Precision:F3})"));

var points = string.Join(" ", sweep.Select(r => FormattableString.Invariant($"{50 + 400 * r.Metrics.Recall:F2},{450 - 400 * r.Metrics.Precision:F2}")));
File.WriteAllText("precision-recall.svg", $"""
<svg xmlns="http://www.w3.org/2000/svg" width="520" height="510" viewBox="0 0 520 510">
  <rect width="520" height="510" fill="white"/>
  <text x="50" y="25">Validation precision-recall (threshold grid)</text>
  <path d="M50 50 V450 H450" fill="none" stroke="black"/>
  <text x="225" y="490">Recall (0 to 1)</text>
  <text x="55" y="45">Precision (0 to 1)</text>
  <polyline points="{points}" fill="none" stroke="steelblue" stroke-width="2"/>
</svg>
""");
Console.WriteLine("Validation chart: precision-recall.svg");

var policies = new[]
{
    ("Balanced F1", SelectionGoal.BalancedF1, 0.0, (int?)null),
    ("Limited resources: precision >= 0.8, prefetch <= 5", SelectionGoal.RecallWithPrecisionFloor, 0.8, (int?)5),
    ("Expensive misses: recall >= 0.8", SelectionGoal.PrecisionWithRecallFloor, 0.8, (int?)null)
};
// Freeze every policy's threshold on validation BEFORE reading held-out test labels.
var selected = policies.Select(p => (p.Item1, Result: MetricSelectionPolicy.Select(sweep, p.Item2, p.Item3, p.Item4))).ToArray();
var test = Load("test.csv");
foreach (var choice in selected)
{
    Console.WriteLine($"\n{choice.Item1}");
    if (choice.Result is null) { Console.WriteLine("No feasible validation threshold."); continue; }
    Console.Write("Validation: "); Print(choice.Result);
    Console.Write("Held-out test (frozen threshold): ");
    Print(new(choice.Result.Threshold, ThresholdEvaluator.Evaluate(test, choice.Result.Threshold)));
}

var imbalanced = Load("imbalanced.csv");
Console.WriteLine("\nImbalanced baseline: never prefetch (all scores zero, threshold 0.5)");
Print(new(0.5, ThresholdEvaluator.Evaluate(imbalanced.Select(p => p with { Score = 0 }), 0.5)));
Console.WriteLine("Accuracy can be 95% while recall and F1 are zero. See README for exercises and interpretation.");

Prediction[] Load(string name) => File.ReadLines(Path.Combine(dataDirectory, name)).Skip(1)
    .Where(line => !string.IsNullOrWhiteSpace(line)).Select(line =>
    {
        var fields = line.Split(',');
        if (fields.Length != 3 || (fields[1] != "0" && fields[1] != "1"))
            throw new FormatException($"Invalid row in {name}: {line}");
        return new Prediction(fields[0], fields[1] == "1", double.Parse(fields[2], CultureInfo.InvariantCulture));
    }).ToArray();

static void Print(ThresholdResult r)
{
    var m = r.Metrics;
    Console.WriteLine(FormattableString.Invariant($"{r.Threshold:F1} {m.TP,2} {m.FP,2} {m.FN,2} {m.TN,2} {m.Prefetched,2} {m.Precision:F3} {m.Recall:F3} {m.F1:F3} {m.Accuracy:F3}"));
}
