namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IPendingRemovalsRegistry
{
    public Task StorePendingRemoval(Guid userId, CancellationTokenSource cts);
    public Task<bool> TryGetPendingRemoval(Guid userId, out CancellationTokenSource cts);
}