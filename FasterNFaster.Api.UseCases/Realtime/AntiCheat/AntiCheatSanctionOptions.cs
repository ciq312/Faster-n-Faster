namespace FasterNFaster.Api.UseCases.Realtime.AntiCheat;

public class AntiCheatSanctionOptions
{
    public int RaceViolationThreshold { get; set; } = 3;
    public int SuspensionThreshold { get; set; } = 3;
    public TimeSpan StrikeWindow { get; set; } = TimeSpan.FromHours(24);
    public TimeSpan SuspensionDuration { get; set; } = TimeSpan.FromHours(24);
}
