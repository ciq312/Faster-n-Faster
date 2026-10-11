using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.UnitTests.Fakes;

public class FakeAntiCheatPolicy(double maxCharsPerSecond, int budgetSlack) : IAntiCheatPolicy
{
    public double MaxCharsPerSecond { get; } = maxCharsPerSecond;
    public int BudgetSlack { get; } = budgetSlack;
}
