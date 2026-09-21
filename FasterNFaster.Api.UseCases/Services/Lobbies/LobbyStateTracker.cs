using System.Collections.Concurrent;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Services;

public sealed class LobbyStateTracker : ILobbyStateTracker
{
    // AsyncLocal writes flow down the async call tree but never back up, so the slot is
    // assigned once per scope and everything below it only mutates the dictionary it points at.
    private readonly AsyncLocal<ConcurrentDictionary<Guid, byte>?> current = new();

    public bool TryBeginScope()
    {
        if (current.Value is not null) return false;

        current.Value = new ConcurrentDictionary<Guid, byte>();
        return true;
    }

    public void MarkChanged(Guid lobbyId) => current.Value?.TryAdd(lobbyId, 0);

    public IReadOnlyCollection<Guid> EndScope()
    {
        var changed = current.Value;
        current.Value = null;

        return changed is null ? [] : changed.Keys.ToArray();
    }
}
