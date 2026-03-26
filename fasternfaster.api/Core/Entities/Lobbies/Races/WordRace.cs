namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public class WordRace : Race
{
    public WordRace(int wordCount)
    {
        if (wordCount <= 0)
            throw new ArgumentException("Word count must be greater than 0.");

        MaxWords = wordCount;
    }
}
