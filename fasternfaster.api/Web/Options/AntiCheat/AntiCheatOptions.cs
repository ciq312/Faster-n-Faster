namespace FasterNFaster.Api.Web.Options.AntiCheat;

public class AntiCheatOptions
{
    public double SustainedMaxWpm { get; set; } = 300;
    public double BurstMaxWpm { get; set; } = 600;
    public int AverageWordLength { get; set; } = 5;
    public int BurstMinIndexDelta { get; set; } = 15;
    public int SustainedCheckMinIndex { get; set; } = 30;
}
