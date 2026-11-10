namespace SkipMaster.Core;
public interface IRandomSource
{
    int Next(int maxExclusive);

    bool NextBool();
}

public sealed class SystemRandomSource : IRandomSource
{
    private readonly Random _random;

    public SystemRandomSource(int? seed = null) =>
        _random = seed is null ? new Random() : new Random(seed.Value);

    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    public bool NextBool() => _random.Next(2) == 1;
}
