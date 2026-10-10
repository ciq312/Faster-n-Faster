namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IPendingRemovalsRegistry
{
    /// <summary>Takes ownership of <paramref name="cts"/>: it may be cancelled and disposed at any time.</summary>
    void StorePendingRemoval(Guid userId, CancellationTokenSource cts);
    bool TryCancelPendingRemoval(Guid userId);

    /// <returns>True if <paramref name="cts"/> was still current; the caller then owns it and must dispose it.</returns>
    bool CompletePendingRemoval(Guid userId, CancellationTokenSource cts);
}
