using SkipMaster.Contracts;

namespace SkipMaster.Strategy;

public sealed class BayesSkipStrategy : ISkipStrategy
{
    private const int SubjectCount = 6;
    private const int SemesterDays = 100;
    private const int FirstDay = 1;

    private const double ExpectedGainPerDay = 5.0;

    private enum Kind { Random, Follow, Xor }

    private readonly record struct Hypothesis(Kind Kind, int A, int B, double Prior);

    private static readonly Hypothesis[] Hypotheses = BuildHypotheses();

    public string Name => "Bayes";

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history)
    {
        int past = day - FirstDay;                       
        var asked = new bool[past + 1, SubjectCount];    
        int skips = 0;

        for (int row = 1; row <= past; row++)
        {
            int d = FirstDay + row - 1;
            for (int s = 0; s < SubjectCount; s++)
            {
                var subject = (Subject)s;
                bool attended = history.Attended(d, subject);
                if (!attended) skips++;
                asked[row, s] = attended && history.WasAsked(d, subject) == true;
            }
        }

        double accumulated = past + skips;
        int remaining = Math.Max(1, SemesterDays - past);
        double stake = accumulated + remaining * ExpectedGainPerDay;

        var attend = new bool[SubjectCount];
        var weights = new double[Hypotheses.Length];

        for (int t = 0; t < SubjectCount; t++)
        {
            double pAsked = ProbabilityAskedToday(t, asked, past, weights);
            attend[t] = pAsked * stake >= 1.0;
        }

        return attend;
    }

    private static double ProbabilityAskedToday(int teacher, bool[,] asked, int past, double[] weights)
    {
        for (int i = 0; i < Hypotheses.Length; i++) weights[i] = Hypotheses[i].Prior;

        for (int row = 1; row <= past; row++)
        {
            bool observed = asked[row, teacher];
            for (int i = 0; i < Hypotheses.Length; i++)
            {
                if (weights[i] == 0.0) continue;
                var h = Hypotheses[i];
                if (h.Kind == Kind.Random) weights[i] *= 0.5;
                else if (Predict(h, asked, row - 1) != observed) weights[i] = 0.0;
            }
        }

        double total = 0, weightedAsk = 0;
        for (int i = 0; i < Hypotheses.Length; i++)
        {
            double w = weights[i];
            if (w == 0.0) continue;
            var h = Hypotheses[i];
            total += w;
            weightedAsk += h.Kind == Kind.Random ? 0.5 * w : (Predict(h, asked, past) ? w : 0.0);
        }

        return total <= 0 ? 1.0 : weightedAsk / total;
    }

    private static bool Predict(Hypothesis h, bool[,] asked, int yesterdayRow) =>
        h.Kind == Kind.Follow
            ? asked[yesterdayRow, h.A]
            : asked[yesterdayRow, h.A] != asked[yesterdayRow, h.B];

    private static Hypothesis[] BuildHypotheses()
    {
        var list = new List<Hypothesis> { new(Kind.Random, 0, 0, 1.0 / 3) };
        for (int a = 0; a < SubjectCount; a++)
            list.Add(new Hypothesis(Kind.Follow, a, 0, 1.0 / 3 / SubjectCount));
        for (int a = 0; a < SubjectCount; a++)
            for (int b = a + 1; b < SubjectCount; b++)
                list.Add(new Hypothesis(Kind.Xor, a, b, 1.0 / 3 / 15));
        return list.ToArray();
    }
}
