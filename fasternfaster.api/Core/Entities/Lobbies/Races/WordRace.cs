using FasterNFaster.Api.Core.Events;
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
        return Participants.Values
            .Where(p => !p.IsFinished)
            .Select(p => new ParticipantSnapshot(p.Id, p.Index, p.Typed, p.GetWPM(), p.Color, p.Nick, p.Mistakes))
            .ToList();
    }

    public override void ProcessUpdate(Guid playerId, int index, int mistakes, string typed)
    {
        if (!HasStarted) return;

        if (Passage == null) throw new NullReferenceException("passage isn't set");

        var racer = Participants.GetValueOrDefault(playerId) ?? throw new UserNotFoundException(playerId);

        racer.UpdateProgress(index, typed, mistakes, Passage);

        if (IsRacerFinished(racer))
        {
            racer.MarkFinished(nextFinishPosition++, GetNumberWordsInPassage());
            RaiseDomainEvent(new PlayerFinishedEvent(racer.Nick, racer.Id, racer.FinishPosition, racer.GetWPM(), racer.GetAccuracy()));
            if (IsRaceFinished())
                OnRaceFinished();
        }
    }

    private bool IsRacerFinished(RaceParticipant racer) => racer.Index >= Passage!.Length - 1;


    public override void Reset()
    {
        base.Reset();
        Passage = null;
        WordCount = 0;
    }
    private int GetNumberWordsInPassage()
    {
        if (Passage == null) throw new NullReferenceException("Passage is null");
        return Passage.Split(' ').Length;
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
