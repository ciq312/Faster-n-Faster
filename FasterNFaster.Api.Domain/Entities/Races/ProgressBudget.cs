namespace FasterNFaster.Api.Core.Entities.Races;

public class ProgressBudget
{
    private readonly double charsPerSecond;
    private readonly double capacity;
    private readonly Func<DateTime> now;
    private double tokens;
    private DateTime lastRefillAt;

    public ProgressBudget(double charsPerSecond, int slack, Func<DateTime> now)
    {
        this.charsPerSecond = charsPerSecond;
        capacity = slack;
        this.now = now;
        tokens = slack;
        lastRefillAt = now();
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
        var currentTime = now();
        var elapsedSeconds = Math.Max(0, (currentTime - lastRefillAt).TotalSeconds);
        lastRefillAt = currentTime;
        tokens = Math.Min(capacity, tokens + elapsedSeconds * charsPerSecond);
    }
}
