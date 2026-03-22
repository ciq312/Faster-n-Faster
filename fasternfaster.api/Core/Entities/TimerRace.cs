using FasterNFaster.Api.Core.Entities.Lobby;

namespace FasterNFaster.Api.Core.Entities;

public class TimerRace : Race
{
    public int TimerDurationSeconds { get; private set; }

    public TimerRace(int timerDurationSeconds)
    {
        if (timerDurationSeconds <= 0)
            throw new ArgumentException("Timer duration must be greater than 0.");

        TimerDurationSeconds = timerDurationSeconds;
        MaxWords = 200; // large pool — timer expires before words run out
    }
}
