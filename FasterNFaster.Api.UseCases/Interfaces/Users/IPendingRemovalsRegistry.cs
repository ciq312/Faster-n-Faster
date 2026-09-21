namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IPendingRemovalsRegistry
{
    public void StorePendingRemoval(Guid userId, CancellationTokenSource cts);
    public bool TryCancelPendingRemoval(Guid userId);

    public void RemovePendingRemoval(Guid userId);
}