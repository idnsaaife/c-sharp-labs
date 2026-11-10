using SkipMaster.Contracts;

namespace SkipMaster.Core;

public static class Subjects
{
    public const int Count = 6;
}

public enum RuleKind
{
    /// спросить с вероятностью 50%
    Random = 1,
    /// спросить если вчера спросили по предмету A
    FollowSubject = 2,
    /// спросить если вчера спросили ровно по одному из предметов A и Б
    ExclusiveOr = 3
}

public interface ITeacherRule
{
    RuleKind Kind { get; }
    Subject? A { get; }
    Subject? B { get; }

    bool ShouldAsk(IReadOnlyList<bool> askedYesterday);
}

public sealed class RandomRule(IRandomSource random) : ITeacherRule
{
    public RuleKind Kind => RuleKind.Random;
    public Subject? A => null;
    public Subject? B => null;

    public bool ShouldAsk(IReadOnlyList<bool> askedYesterday) => random.NextBool();
}

public sealed class FollowRule(Subject a) : ITeacherRule
{
    public RuleKind Kind => RuleKind.FollowSubject;
    public Subject? A => a;
    public Subject? B => null;

    public bool ShouldAsk(IReadOnlyList<bool> askedYesterday) => askedYesterday[(int)a];
}

public sealed class ExclusiveOrRule : ITeacherRule
{
    private readonly Subject _a, _b;

    public ExclusiveOrRule(Subject a, Subject b)
    {
        if (a == b) throw new ArgumentException("Subjects A and B must be different.");
        _a = a;
        _b = b;
    }

    public RuleKind Kind => RuleKind.ExclusiveOr;
    public Subject? A => _a;
    public Subject? B => _b;

    public bool ShouldAsk(IReadOnlyList<bool> askedYesterday) =>
        askedYesterday[(int)_a] != askedYesterday[(int)_b];
}

public interface ITeacherRuleFactory
{
    ITeacherRule[] CreateForSemester();
}

public sealed class TeacherRuleFactory(IRandomSource random) : ITeacherRuleFactory
{
    public ITeacherRule[] CreateForSemester()
    {
        var rules = new ITeacherRule[Subjects.Count];
        for (int i = 0; i < rules.Length; i++)
        {
            var kind = (RuleKind)(random.Next(3) + 1);
            switch (kind)
            {
                case RuleKind.Random:
                    rules[i] = new RandomRule(random);
                    break;
                case RuleKind.FollowSubject:
                    rules[i] = new FollowRule((Subject)random.Next(Subjects.Count));
                    break;
                default:
                    int a = random.Next(Subjects.Count);
                    int b = random.Next(Subjects.Count - 1);
                    if (b >= a) b++; 
                    rules[i] = new ExclusiveOrRule((Subject)a, (Subject)b);
                    break;
            }
        }
        return rules;
    }
}
