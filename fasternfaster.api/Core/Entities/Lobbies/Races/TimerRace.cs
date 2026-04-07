namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public class TimerRace : Race
{
    public int TimerDurationSeconds { get; private set; }

    public TimerRace(int timerDurationSeconds)
    {
        if (timerDurationSeconds <= 0)
            throw new ArgumentException("Timer duration must be greater than 0.");

        TimerDurationSeconds = timerDurationSeconds;
    }

    public override void ProcessUpdate(Guid playerId, int index, int mistakes)
    {
        throw new NotImplementedException();
    }

    public override List<ParticipantSnapshot> GetSnapshot()
    {
        throw new NotImplementedException();
    }

    public override IRaceSettings GetRaceSettings()
    {
        throw new NotImplementedException();
    }
}
