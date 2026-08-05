using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Web.Options.AntiCheat;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Api.Web.Services.Implementations;

public class ConfiguredAntiCheatPolicy(IOptions<AntiCheatOptions> options) : IAntiCheatPolicy
{
    private readonly AntiCheatOptions opts = options.Value;

    public double SustainedMaxWpm => opts.SustainedMaxWpm;
    public double BurstMaxWpm => opts.BurstMaxWpm;
    public int AverageWordLength => opts.AverageWordLength;
    public int BurstMinIndexDelta => opts.BurstMinIndexDelta;
    public int SustainedCheckMinIndex => opts.SustainedCheckMinIndex;
    public double BurstMaxCharsPerSecond => opts.BurstMaxWpm * opts.AverageWordLength / 60.0;
}
