using System.Text;
using System.Globalization;
using SkipMaster.Contracts;
using SkipMaster.Core;
using SkipMaster.Core.Strategies;
using SkipMaster.Strategy;

Console.OutputEncoding = Encoding.UTF8;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

string strategyName = GetArg(args, "--strategy") ?? "bayes";
int? seed = int.TryParse(GetArg(args, "--seed"), out var seedValue) ? seedValue : null;
int runs = int.TryParse(GetArg(args, "--runs"), out var runsValue) && runsValue > 0 ? runsValue : 1;

ISkipStrategy strategy = strategyName.ToLowerInvariant() switch
{
    "bayes" => new BayesSkipStrategy(),
    "always" => new AlwaysAttendStrategy(),
    _ => throw new ArgumentException($"Unknown strategy '{strategyName}'. Available strategies: bayes, always.")
};

var options = new SemesterOptions();
var totals = new List<int>();
int expulsions = 0;

for (int i = 0; i < runs; i++)
{
    var random = new SystemRandomSource(seed is null ? null : seed + i);
    var simulator = new SemesterSimulator(strategy, new TeacherRuleFactory(random), options);

    while (!simulator.IsFinished)
        simulator.PlayNextDay();

    totals.Add(simulator.TotalPleasure);
    if (simulator.IsExpelled) expulsions++;

    if (runs == 1)
    {
        Console.WriteLine($"Strategy: {strategy.Name}");
        Console.WriteLine(simulator.IsExpelled
            ? $"The student was expelled on day {simulator.DaysPlayed}. Pleasure was reset to zero."
            : $"Semester completed ({simulator.DaysPlayed} days).");
        Console.WriteLine($"Total pleasure: {simulator.TotalPleasure}");
        Console.WriteLine($"Average pleasure per day: {simulator.AveragePerDay:F2}");
    }
}

if (runs > 1)
{
    Console.WriteLine($"Strategy: {strategy.Name}, runs: {runs}");
    Console.WriteLine($"Average pleasure per semester: {totals.Average():F2}");
    Console.WriteLine($"Average pleasure per day: {totals.Average() / options.Days:F2}");
    Console.WriteLine($"Expulsions: {expulsions} ({100.0 * expulsions / runs:F1}%)");
}

static string? GetArg(string[] a, string name)
{
    int i = Array.IndexOf(a, name);
    return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
}
