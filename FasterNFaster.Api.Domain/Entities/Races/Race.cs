using FasterNFaster.Api.Domain.Entities.Keys;
using FasterNFaster.Api.Domain.Entities.Lobbies.Events;
using FasterNFaster.Api.Domain.Entities.Races.Events;
using FasterNFaster.Api.Domain.Exceptions.Races;

namespace FasterNFaster.Api.Domain.Entities.Races;

public record struct ParticipantSnapshot(Guid PlayerId, int Index, double Wpm);

public sealed class Race : AggregateRoot<Guid>
{
    public Guid LobbyId { get; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public bool HasStarted { get; private set; }
    public int WordCount { get; }
    public string Passage { get; private set; }

    private readonly Dictionary<Guid, RaceParticipant> participants = new();
    public IReadOnlyDictionary<Guid, RaceParticipant> Participants => participants;

    private readonly TimeProvider timeProvider;
    private int nextFinishPosition = 1;

    public Race(Guid lobbyId, int wordCount, string passage, TimeProvider timeProvider)
    {
        if (wordCount <= 0)
            throw new ArgumentException("Word count must be greater than 0.");
        ArgumentException.ThrowIfNullOrEmpty(passage);

        Id = Guid.NewGuid();
        LobbyId = lobbyId;
        WordCount = wordCount;
        Passage = passage;
        this.timeProvider = timeProvider;
    }

    public void AddParticipant(RaceParticipant participant)
    {
        if (HasStarted) throw new InvalidOperationException("Cannot add participants to a started race.");
        participants[participant.Id] = participant;
    }

    public void AddParticipants(IEnumerable<RaceParticipant> newParticipants)
    {
        foreach (var participant in newParticipants)
            AddParticipant(participant);
    }

    public void Start()
    {
        if (HasStarted) throw new InvalidOperationException("Race already started.");
        StartTime = timeProvider.GetUtcNow();
        HasStarted = true;
        foreach (var participant in participants.Values)
        {
            participant.Start();
        }
    }

    public List<ParticipantSnapshot> GetSnapshot() =>
        [.. Participants.Values
            .Where(p => !p.IsFinished)
            .Select(p => new ParticipantSnapshot(p.Id, p.LastValidIndex, p.GetWPM()))];

    public void ProcessUpdate(Guid playerId, Keystroke[] keystrokes)
    {
        if (!HasStarted) return;

        var racer = Participants.GetValueOrDefault(playerId) ?? throw new PlayerNotInRaceException(playerId);
        if (racer.IsFinished) return;

        var outcome = racer.UpdateProgress(keystrokes, Passage);
        HandleOutcome(racer, outcome);
    }

    public IEnumerable<RaceParticipantResult> GetRaceResults() =>
        participants.Values
            .Where(p => p.Result != null)
            .Select(p => p.Result!);

    public void WithdrawParticipant(Guid playerId)
    {
        var participant = participants.GetValueOrDefault(playerId);
        if (participant is null || participant.IsFinished) return;
        participant.MarkWithdrawn();
        if (IsRaceFinished())
            FinishRace();
    }

    public bool TryChangePassage(string passage)
    {
        if (HasStarted) return false;

        Passage = passage;
        return true;
    }

    public void RetryPendingClaims()
    {
        if (!HasStarted) return;

        foreach (var racer in Participants.Values.ToList())
        {
            HandleOutcome(racer, racer.RetryPendingClaim(Passage));
        }
    }

    public bool IsRaceFinished() => participants.Values.All(p => p.IsFinished);

    private void HandleOutcome(RaceParticipant racer, ProgressOutcome outcome)
    {
        if (outcome.Status == ProgressStatus.Rejected)
        {
            RaiseDomainEvent(new RaceViolationEvent(LobbyId, racer.Id, racer.Nick, outcome.Rule!));
            return;
        }

        if (DidRacerFinish(racer) && !racer.IsFinished)
        {
            racer.MarkFinished(nextFinishPosition++);
            RaiseDomainEvent(new PlayerFinishedEvent(LobbyId, racer.Nick, racer.Id, racer.FinishPosition, racer.Result!.WPM, racer.Result.Accuracy));
            if (IsRaceFinished())
                FinishRace();
        }
    }

    private void FinishRace()
    {
        RaiseDomainEvent(new RaceFinishedEvent(LobbyId, GetRaceResults().ToList()));
        EndTime = timeProvider.GetUtcNow();
        Reset();
    }

    private void Reset()
    {
        participants.Clear();
        nextFinishPosition = 1;
        HasStarted = false;
    }

    private bool DidRacerFinish(RaceParticipant racer) => racer.LastValidIndex >= Passage.Length - 1;
}
