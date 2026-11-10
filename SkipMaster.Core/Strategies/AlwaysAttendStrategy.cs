using SkipMaster.Contracts;

namespace SkipMaster.Core.Strategies;

/// всегда посещает все пары - получает только пирожки
public sealed class AlwaysAttendStrategy : ISkipStrategy
{
    public string Name => "AlwaysAttend";

    public bool[] DecideDay(int day, IReadOnlyStudentHistory history) =>
        Enumerable.Repeat(true, Subjects.Count).ToArray();
}
