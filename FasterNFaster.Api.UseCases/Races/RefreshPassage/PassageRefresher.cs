using FasterNFaster.Api.UseCases.Races.Ports;

namespace FasterNFaster.Api.UseCases.Races.RefreshPassage;

public class PassageRefresher(IRaceStore races, IPassageProvider passageProvider)
{
    public async Task<string> FetchNext(Guid lobbyId)
    {
        var wordCount = await races.Read(lobbyId, race => race.WordCount);

        return await passageProvider.GetPassageAsync(wordCount);
    }

    public async Task<bool> TryApply(Guid lobbyId, string passage)
    {
        var changed = false;
        await races.TryMutate(lobbyId, race => changed = race.TryChangePassage(passage));

        return changed;
    }
}
