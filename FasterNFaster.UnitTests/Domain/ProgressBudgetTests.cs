using FasterNFaster.Api.Domain.Entities.Races;
using Microsoft.Extensions.Time.Testing;

namespace FasterNFaster.UnitTests.Domain;

public class ProgressBudgetTests
{
    [Fact]
    public void Take_StartsFull_GrantsUpToSlack()
    {
        var clock = new FakeTimeProvider();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock);

        Assert.Equal(6, budget.Take(10));
    }

    [Fact]
    public void Take_WhenEmpty_GrantsNothing()
    {
        var clock = new FakeTimeProvider();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock);
        budget.Take(6);

        Assert.Equal(0, budget.Take(1));
    }

    [Fact]
    public void Take_AfterRefill_GrantsCharsPerSecondTimesElapsedSeconds()
    {
        var clock = new FakeTimeProvider();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 10, clock);
        budget.Take(10);

        clock.Advance(TimeSpan.FromSeconds(1.5));

        Assert.Equal(3, budget.Take(10));
    }

    [Fact]
    public void Take_RefillCapsAtSlack()
    {
        var clock = new FakeTimeProvider();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock);
        budget.Take(6);

        clock.Advance(TimeSpan.FromMinutes(10));

        Assert.Equal(6, budget.Take(100));
    }

    [Fact]
    public void Take_FractionalRefill_AccumulatesUntilWholeCharacter()
    {
        var clock = new FakeTimeProvider();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock);
        budget.Take(6);

        clock.Advance(TimeSpan.FromSeconds(0.25));
        Assert.Equal(0, budget.Take(1));

        clock.Advance(TimeSpan.FromSeconds(0.25));
        Assert.Equal(1, budget.Take(1));
    }

    [Fact]
    public void Take_ClockMovesBackwards_DoesNotRefill()
    {
        var clock = new RewindableTimeProvider();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock);
        budget.Take(6);

        clock.Now -= TimeSpan.FromSeconds(10);

        Assert.Equal(0, budget.Take(1));
    }

    [Fact]
    public void Take_NegativeRequest_GrantsNothingAndKeepsTokens()
    {
        var clock = new FakeTimeProvider();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock);

        Assert.Equal(0, budget.Take(-4));
        Assert.Equal(6, budget.Take(6));
    }

    // FakeTimeProvider.Advance and SetUtcNow throw when moved backwards.
    private sealed class RewindableTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
