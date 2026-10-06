using FasterNFaster.Api.Web.Options.AntiCheat;
using FasterNFaster.Api.Web.Services.Implementations;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Services;

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
}
