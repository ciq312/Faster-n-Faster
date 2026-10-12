using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.Api.UseCases.Races.RefreshPassage;

public class PassageRefresher(IRaceStore races, IPassageProvider passageProvider, ILobbyStateTracker tracker)
{
    public async Task<bool> TryRefresh(Guid lobbyId)
    {
        var wordCount = await races.Read(lobbyId, race => race.WordCount);

        var passage = await passageProvider.GetPassageAsync(wordCount);

        var changed = false;
        await races.Mutate(lobbyId, race => changed = race.TryChangePassage(passage));
        if (changed) tracker.MarkChanged(lobbyId);

        return changed;
    }
}
