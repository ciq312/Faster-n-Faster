using FasterNFaster.Api.UseCases.Exceptions;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public class WordRace : Race
{
    public int WordCount { get; private set; }
    public string? Passage { get; private set; }
    public WordRace(int wordCount)
    {
        if (wordCount <= 0)
            throw new ArgumentException("Word count must be greater than 0.");

        WordCount = wordCount;
    }

    public override List<ParticipantSnapshot> GetSnapshot()
    {
        // Enumerate under the race lock; ProcessUpdate mutates participant state on hub threads.
        lock (_raceLock)
        {
            return Participants.Values
                .Where(p => !p.IsFinished)
                .Select(p => new ParticipantSnapshot(p.Id, p.Index, p.Typed, p.GetWPM(), p.Color, p.Nick, p.Mistakes))
                .ToList();
        }
    }

    public override void ProcessUpdate(Guid playerId, int index, int mistakes, string typed)
    {
        lock (_raceLock)
        {
            if (!HasStarted) return;
            if (Passage == null) throw new NullReferenceException("passage isn't set");
            var racer = Participants.GetValueOrDefault(playerId) ?? throw new UserNotFoundException(playerId);

            racer.UpdateProgress(index, typed, mistakes, Passage);

            if (!racer.IsFinished && racer.Index >= Passage.Length - 1)
                racer.MarkFinished(_nextFinishPosition++, Passage.Split(' ').Length);
        }
    }

    public void SetPassage(string passage)
    {
        Passage = passage;
    }

    public override WordRaceSettings GetRaceSettings()
    {
        if (Passage == null) throw new NullReferenceException("Passage is null");
        return new WordRaceSettings(Passage, WordCount);
    }

    public record WordRaceSettings(string Passage, int WordCount) : IRaceSettings;
}
