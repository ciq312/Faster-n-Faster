using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Tests.Fakes;

namespace FasterNFaster.Tests.Entities;

public class ProgressBudgetTests
{
    [Fact]
    public void Take_StartsFull_GrantsUpToSlack()
    {
        var clock = new FakeClock();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock.Func);

        Assert.Equal(6, budget.Take(10));
    }

    [Fact]
    public void Take_WhenEmpty_GrantsNothing()
    {
        var clock = new FakeClock();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock.Func);
        budget.Take(6);

        Assert.Equal(0, budget.Take(1));
    }

    [Fact]
    public void Take_AfterRefill_GrantsCharsPerSecondTimesElapsedSeconds()
    {
        var clock = new FakeClock();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 10, clock.Func);
        budget.Take(10);

        clock.Advance(TimeSpan.FromSeconds(1.5));

        Assert.Equal(3, budget.Take(10));
    }

    [Fact]
    public void Take_RefillCapsAtSlack()
    {
        var clock = new FakeClock();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock.Func);
        budget.Take(6);

        clock.Advance(TimeSpan.FromMinutes(10));

        Assert.Equal(6, budget.Take(100));
    }

    [Fact]
    public void Take_FractionalRefill_AccumulatesUntilWholeCharacter()
    {
        var clock = new FakeClock();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock.Func);
        budget.Take(6);

        clock.Advance(TimeSpan.FromSeconds(0.25));
        Assert.Equal(0, budget.Take(1));

        clock.Advance(TimeSpan.FromSeconds(0.25));
        Assert.Equal(1, budget.Take(1));
    }

    [Fact]
    public void Take_ClockMovesBackwards_DoesNotRefill()
    {
        var clock = new FakeClock();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock.Func);
        budget.Take(6);

        clock.Advance(TimeSpan.FromSeconds(-10));

        Assert.Equal(0, budget.Take(1));
    }

    [Fact]
    public void Take_NegativeRequest_GrantsNothingAndKeepsTokens()
    {
        var clock = new FakeClock();
        var budget = new ProgressBudget(charsPerSecond: 2, slack: 6, clock.Func);

        Assert.Equal(0, budget.Take(-4));
        Assert.Equal(6, budget.Take(6));
    }
}
