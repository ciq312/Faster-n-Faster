namespace FasterNFaster.Api.Domain.Entities.Races;

public class ProgressBudget
{
    private readonly double charsPerSecond;
    private readonly double capacity;
    private readonly TimeProvider timeProvider;
    private double tokens;
    private DateTimeOffset lastRefillAt;

    public ProgressBudget(double charsPerSecond, int slack, TimeProvider timeProvider)
    {
        this.charsPerSecond = charsPerSecond;
        capacity = slack;
        this.timeProvider = timeProvider;
        tokens = slack;
        lastRefillAt = timeProvider.GetUtcNow();
    }

    public int Take(int requested)
    {
        Refill();
        var granted = Math.Clamp(requested, 0, (int)Math.Floor(tokens));
        tokens -= granted;
        return granted;
    }

    private void Refill()
    {
        var currentTime = timeProvider.GetUtcNow();
        var elapsedSeconds = Math.Max(0, (currentTime - lastRefillAt).TotalSeconds);
        lastRefillAt = currentTime;
        tokens = Math.Min(capacity, tokens + elapsedSeconds * charsPerSecond);
    }
}
