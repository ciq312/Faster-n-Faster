using FasterNFaster.Api.Core.Entities.Lobby;

namespace FasterNFaster.Api.Core.Entities;

public class WordRace : Race
{
    public WordRace(int wordCount)
    {
        if (wordCount <= 0)
            throw new ArgumentException("Word count must be greater than 0.");

        MaxWords = wordCount;
    }
}
