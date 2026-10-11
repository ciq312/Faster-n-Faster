using FasterNFaster.Api.Domain.Interfaces;
using FasterNFaster.Api.Presentation.Web.Options.AntiCheat;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Presentation.Web.Services.Implementations;

public class ConfiguredAntiCheatPolicy(IOptions<AntiCheatOptions> options) : IAntiCheatPolicy
{
    private readonly AntiCheatOptions opts = options.Value;

    public double MaxCharsPerSecond => opts.MaxWpm * opts.AverageWordLength / 60.0;
    public int BudgetSlack => opts.BudgetSlack;
}
