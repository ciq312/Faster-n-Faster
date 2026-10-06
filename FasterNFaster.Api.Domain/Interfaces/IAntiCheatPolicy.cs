namespace FasterNFaster.Api.Core.Interfaces;

public interface IAntiCheatPolicy
{
    double MaxCharsPerSecond { get; }
    int BudgetSlack { get; }
}
