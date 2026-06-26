namespace FasterNFaster.Api.Core.Interfaces;

public interface IAntiCheatPolicy
{
    double SustainedMaxWpm { get; }
    double BurstMaxWpm { get; }
    int AverageWordLength { get; }
    int BurstMinIndexDelta { get; }
    int SustainedCheckMinIndex { get; }
    double BurstMaxCharsPerSecond { get; }
}
