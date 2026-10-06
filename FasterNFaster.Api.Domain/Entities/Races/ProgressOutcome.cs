namespace FasterNFaster.Api.Core.Entities.Races;

public enum ProgressStatus
{
    Accepted,
    Clamped,
    Rejected
}

public readonly record struct ProgressOutcome(ProgressStatus Status, string? Rule = null)
{
    public static ProgressOutcome Accepted { get; } = new(ProgressStatus.Accepted);
    public static ProgressOutcome Clamped { get; } = new(ProgressStatus.Clamped);
    public static ProgressOutcome Rejected(string rule) => new(ProgressStatus.Rejected, rule);
}
