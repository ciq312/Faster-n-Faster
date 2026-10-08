namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IPendingRemovalsRegistry
{
    void StorePendingRemoval(Guid userId, CancellationTokenSource cts);
    bool TryCancelPendingRemoval(Guid userId);

    void RemovePendingRemoval(Guid userId);
}
