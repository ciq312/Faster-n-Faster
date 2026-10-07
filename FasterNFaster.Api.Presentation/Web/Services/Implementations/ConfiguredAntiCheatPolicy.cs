using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Web.Options.AntiCheat;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class ConfiguredAntiCheatPolicy(IOptions<AntiCheatOptions> options) : IAntiCheatPolicy
{
    private readonly AntiCheatOptions opts = options.Value;

    public double MaxCharsPerSecond => opts.MaxWpm * opts.AverageWordLength / 60.0;
    public int BudgetSlack => opts.BudgetSlack;
}
