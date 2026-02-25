namespace RacePoker.Core.Engine;

public class SystemRng : IRng
{
    private readonly Random _random = new();

    public int Next(int maxExclusive) => _random.Next(maxExclusive);
}
