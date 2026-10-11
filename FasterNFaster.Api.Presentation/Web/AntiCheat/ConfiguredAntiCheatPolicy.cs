using FasterNFaster.Api.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Presentation.Web.AntiCheat;

public class ConfiguredAntiCheatPolicy(IOptions<AntiCheatOptions> options) : IAntiCheatPolicy
{
    private readonly AntiCheatOptions opts = options.Value;

    public double MaxCharsPerSecond => opts.MaxWpm * opts.AverageWordLength / 60.0;
    public int BudgetSlack => opts.BudgetSlack;
}
