using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.Api.UseCases.Races.RefreshPassage;

public class PassageRefresher(
    ILobbyStore lobbies,
    IRaceStore races,
    IPassageProvider passageProvider,
    ILobbyStateTracker tracker)
{
    public async Task<bool> TryRefresh(Guid lobbyId)
    {
        var wordCount = await races.Read(lobbyId, race => race.WordCount);

        var passage = await passageProvider.GetPassageAsync(wordCount);

        var lobbyIdle = await lobbies.TryRead(lobbyId, l => !l.IsSessionActive);
        if (!lobbyIdle) return false;

        var changed = false;
        await races.TryMutate(lobbyId, race => changed = race.TryChangePassage(passage));
        if (changed) tracker.MarkChanged(lobbyId);

        return changed;
    }
}
