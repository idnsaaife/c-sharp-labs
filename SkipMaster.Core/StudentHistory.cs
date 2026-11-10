using SkipMaster.Contracts;

namespace SkipMaster.Core;

public sealed class StudentHistory : IReadOnlyStudentHistory
{
    private readonly List<bool[]> _attended = new();
    private readonly List<bool[]> _asked = new();

    public int DaysRecorded => _attended.Count;

    public void Record(int day, IReadOnlyList<bool> attended, IReadOnlyList<bool> asked)
    {
        if (attended.Count != Subjects.Count || asked.Count != Subjects.Count)
            throw new ArgumentException($"Each day must contain data for all {Subjects.Count} subjects.");

        if (day != DaysRecorded + 1)
            throw new InvalidOperationException($"Expected day {DaysRecorded + 1}, but received day {day}.");
        _attended.Add(attended.ToArray());
        _asked.Add(asked.ToArray());
    }

    public bool Attended(int day, Subject subject) => _attended[Index(day)][(int)subject];

    public bool? WasAsked(int day, Subject subject)
    {
        int i = Index(day);
        return _attended[i][(int)subject] ? _asked[i][(int)subject] : null;
    }

    private int Index(int day)
    {
        if (day < 1 || day > DaysRecorded) throw new ArgumentOutOfRangeException(nameof(day));
        return day - 1;
    }
}
