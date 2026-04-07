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
        List<ParticipantSnapshot> snapshot = new();
        foreach (var participant in Participants.Values) snapshot.Add(new ParticipantSnapshot(participant.Id, participant.Index, participant.GetWPM(), participant.Color, participant.Nick));

        return snapshot;
    }

    public override void ProcessUpdate(Guid playerId, int index, int mistakes)
    {
        lock (_raceLock)
        {
            if (Passage == null) throw new NullReferenceException("passage isn't set");
            var racer = Participants.GetValueOrDefault(playerId) ?? throw new UserNotFoundException(playerId);

            if (!racer.ValidateUpdate(index, mistakes, Passage.Length)) throw new InvalidDataException("Invalid update");

            if (!racer.IsFinished && racer.Index >= Passage.Length - 1)
                racer.MarkFinished(_nextFinishPosition++, Passage.Split(' ').Length);
        }
    }

    public void SetPassage(string passage)
    {
        Passage = passage;
    }

    public override IRaceSettings GetRaceSettings()
    {
        if (Passage == null) throw new NullReferenceException("Passage is null");
        return new WordRaceSettings(Passage, WordCount);
    }

    public record WordRaceSettings(string Passage, int WordCount) : IRaceSettings;
}
