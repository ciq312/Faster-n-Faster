namespace FasterNFaster.Api.Web.Options.AntiCheat;

public class AntiCheatOptions
{
    public double MaxWpm { get; set; } = 350;
    public int AverageWordLength { get; set; } = 5;
    public int BudgetSlack { get; set; } = 6;
}
