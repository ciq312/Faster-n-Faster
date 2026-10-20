namespace FasterNFaster.Api.Domain.Interfaces;

public interface IAntiCheatPolicy
{
    double MaxCharsPerSecond { get; }
    int BudgetSlack { get; }
}
