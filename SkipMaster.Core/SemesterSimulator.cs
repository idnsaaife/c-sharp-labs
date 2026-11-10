using SkipMaster.Contracts;

namespace SkipMaster.Core;

public sealed record SemesterOptions(int Days = 100);

public sealed record DayResult(
    int Day,
    IReadOnlyList<bool> Attended,
    IReadOnlyList<bool> Asked,
    bool Expelled,
    int GainedToday,
    int TotalPleasure);

public interface ISemesterSimulator
{
    int SemesterNumber { get; }
    int DaysPlayed { get; }
    int TotalPleasure { get; }
    double AveragePerDay { get; }
    bool IsExpelled { get; }
    bool IsFinished { get; }
    IReadOnlyList<ITeacherRule> Rules { get; }

    DayResult PlayNextDay();
    void StartNewSemester();
}

public sealed class SemesterSimulator : ISemesterSimulator
{
    private readonly ISkipStrategy _strategy;
    private readonly ITeacherRuleFactory _ruleFactory;
    private readonly SemesterOptions _options;

    private ITeacherRule[] _rules = [];
    private StudentHistory _history = new();
    private bool[] _askedYesterday = new bool[Subjects.Count];

    public SemesterSimulator(ISkipStrategy strategy, ITeacherRuleFactory ruleFactory, SemesterOptions options)
    {
        if (options.Days <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Duration of semester must be > 0");

        _strategy = strategy;
        _ruleFactory = ruleFactory;
        _options = options;
        StartNewSemester();
    }

    public int SemesterNumber { get; private set; }
    public int DaysPlayed { get; private set; }
    public int TotalPleasure { get; private set; }
    public double AveragePerDay => DaysPlayed == 0 ? 0 : (double)TotalPleasure / DaysPlayed;
    public bool IsExpelled { get; private set; }
    public bool IsFinished => IsExpelled || DaysPlayed >= _options.Days;
    public IReadOnlyList<ITeacherRule> Rules => _rules;

    public void StartNewSemester()
    {
        var rules = _ruleFactory.CreateForSemester();
        if (rules.Length != Subjects.Count)
            throw new InvalidOperationException($"Not enough subjects rules for {Subjects.Count} teachers");

        _rules = rules;
        _history = new StudentHistory();
        _askedYesterday = new bool[Subjects.Count]; 
        DaysPlayed = 0;
        TotalPleasure = 0;
        IsExpelled = false;
        SemesterNumber++;
    }

    public DayResult PlayNextDay()
    {
        if (IsFinished) throw new InvalidOperationException("Semester ended");

        int day = DaysPlayed + 1;
        bool[] attend = _strategy.DecideDay(day, _history);
        if (attend is null || attend.Length != Subjects.Count)
            throw new InvalidOperationException($"The strategy must return one decision for each of the {Subjects.Count} subjects.");

        var asked = new bool[Subjects.Count];
        for (int s = 0; s < asked.Length; s++)
            asked[s] = _rules[s].ShouldAsk(_askedYesterday);

        bool expelled = false;
        int skipped = 0;
        for (int s = 0; s < asked.Length; s++)
        {
            if (attend[s]) continue;
            skipped++;
            if (asked[s]) expelled = true;
        }

        int gained;
        if (expelled)
        {
            TotalPleasure = 0; 
            gained = 0;
            IsExpelled = true;
        }
        else
        {
            gained = skipped + 1; 
            TotalPleasure += gained;
        }

        _history.Record(day, attend, asked);
        _askedYesterday = asked;
        DaysPlayed = day;

        return new DayResult(day, attend, asked, expelled, gained, TotalPleasure);
    }
}
