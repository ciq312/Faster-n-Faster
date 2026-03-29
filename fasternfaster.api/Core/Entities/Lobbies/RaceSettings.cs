using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class RaceSettings
{
    public string GameMode { get; private set; } = "wordcount";
    public int WordCount { get; private set; } = 50;
    public int TimerDuration { get; private set; } = 60;
    public string? CustomPassage { get; private set; }

    public void SetGameMode(string gameMode)
    {
        var normalized = gameMode.ToLowerInvariant();
        if (normalized is not ("wordcount" or "timer"))
            throw new ArgumentException($"Unknown game mode: {gameMode}");

        GameMode = normalized;
    }

    public void SetWordCount(int wordCount)
    {
        if (wordCount <= 0)
            throw new ArgumentException("Word count must be greater than 0.");

        WordCount = wordCount;
    }

    public void SetTimerDuration(int duration)
    {
        if (duration <= 0)
            throw new ArgumentException("Timer duration must be greater than 0.");

        TimerDuration = duration;
    }

    public void SetCustomPassage(string? passage)
    {
        CustomPassage = string.IsNullOrWhiteSpace(passage) ? null : passage;
    }

    public Race BuildRace()
    {
        var race = GameMode switch
        {
            "wordcount" => (Race)new WordRace(WordCount),
            "timer" => new TimerRace(TimerDuration),
            _ => throw new InvalidOperationException($"Unknown game mode: {GameMode}")
        };

        if (CustomPassage != null)
            race.SetCustomPassage(CustomPassage);

        return race;
    }
}
