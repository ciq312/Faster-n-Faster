namespace FasterNFaster.Api.Core.Entities;

public class TimerRace : IRace
{
    public string GameMode => "timer";
    public int TimerDurationSeconds { get; private set; }

    private TimerRace() { } // EF constructor

    public TimerRace(int timerDurationSeconds)
    {
        if (timerDurationSeconds <= 0)
            throw new ArgumentException("Timer duration must be greater than 0.");

        TimerDurationSeconds = timerDurationSeconds;
    }
}
