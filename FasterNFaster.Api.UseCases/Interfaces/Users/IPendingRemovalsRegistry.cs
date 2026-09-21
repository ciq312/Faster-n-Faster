namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IPendingRemovalsRegistry
{
    public Task StorePendingRemoval(Guid userId, CancellationTokenSource cts);
    public Task<bool> TryCancelPendingRemoval(Guid userId);

    public Task RemovePendingRemoval(Guid userId);
}