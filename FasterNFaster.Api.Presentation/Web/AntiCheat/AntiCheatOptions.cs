namespace FasterNFaster.Api.Presentation.Web.AntiCheat;

public class AntiCheatOptions
{
    public double MaxWpm { get; set; } = 350;
    public int AverageWordLength { get; set; } = 5;
    public int BudgetSlack { get; set; } = 6;
    public double MaxBacklogSeconds { get; set; } = 2;
}
