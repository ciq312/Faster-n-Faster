using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.Api.UseCases.Races.RefreshPassage;

public class PassageRefresher(IRaceStore races, IPassageProvider passageProvider, ILobbyStateTracker tracker)
{
    public async Task Refresh(Guid lobbyId)
    {
        var wordCount = await races.Read(lobbyId, race => race.WordCount);

        var passage = await passageProvider.GetPassageAsync(wordCount);

        await races.Mutate(lobbyId, race => race.ApplyPassage(passage));

        tracker.MarkChanged(lobbyId);
    }
}
