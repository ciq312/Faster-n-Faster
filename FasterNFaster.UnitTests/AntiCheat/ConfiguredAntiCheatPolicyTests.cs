using FasterNFaster.Api.Presentation.Web.AntiCheat;
using Microsoft.Extensions.Options;
using Shouldly;

namespace FasterNFaster.UnitTests.AntiCheat;

public class ConfiguredAntiCheatPolicyTests
{
    [Fact]
    public void DefaultOptions_MaxCharsPerSecond_Is350WpmInCharacters()
    {
        var policy = new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));

        Assert.Equal(350 * 5 / 60.0, policy.MaxCharsPerSecond, precision: 6);
    }

    [Fact]
    public void DefaultOptions_BudgetSlack_IsOneWord()
    {
        var policy = new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));

        Assert.Equal(6, policy.BudgetSlack);
    }

    [Fact]
    public void DefaultOptions_MaxPendingKeystrokes_IsTwoSecondsOfBudget()
    {
        var policy = new ConfiguredAntiCheatPolicy(Options.Create(new AntiCheatOptions()));

        policy.MaxPendingKeystrokes.ShouldBe(58);
    }
}
