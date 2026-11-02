using FasterNFaster.Api.Domain.Interfaces;
using Moq;

namespace FasterNFaster.UnitTests.Shared;

public static class AntiCheatPolicy
{
    public static IAntiCheatPolicy Unlimited => WithBudget(charsPerSecond: 1000, slack: 1000);

    public static IAntiCheatPolicy WithBudget(double charsPerSecond, int slack, int maxPendingKeystrokes = 1000) =>
        Mock.Of<IAntiCheatPolicy>(p =>
            p.MaxCharsPerSecond == charsPerSecond &&
            p.BudgetSlack == slack &&
            p.MaxPendingKeystrokes == maxPendingKeystrokes);
}
