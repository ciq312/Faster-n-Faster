namespace FasterNFaster.Api.Core.Entities;

public class WordRace : IRace
{
    public string GameMode => "word_count";
    public int WordCount { get; private set; }

    private WordRace() { } // EF constructor

    public WordRace(int wordCount)
    {
        if (wordCount <= 0)
            throw new ArgumentException("Word count must be greater than 0.");

        WordCount = wordCount;
    }
}
