using FasterNFaster.Api.Core.Exceptions.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public static class AntiCheatCheck
{
    public static void ValidateWPM(RaceParticipant participant, int newIndex, IAntiCheatPolicy policy, DateTime now)
    {
        CheckBurstSpeed(participant, newIndex, policy, now);
        CheckSustainedSpeed(participant, newIndex, policy, now);
    }

    private static void CheckBurstSpeed(RaceParticipant participant, int newIndex, IAntiCheatPolicy policy, DateTime now)
    {
        int indexDelta = newIndex - participant.Index;
        double secondsElapsed = (now - participant.LastUpdateAt).TotalSeconds;

        if (secondsElapsed <= 0 || indexDelta < policy.BurstMinIndexDelta) return;

        double charsPerSecond = indexDelta / secondsElapsed;
        if (charsPerSecond > policy.BurstMaxCharsPerSecond)
            throw new CheaterDetectedException("Burst wpm");
    }

    private static void CheckSustainedSpeed(RaceParticipant participant, int newIndex, IAntiCheatPolicy policy, DateTime now)
    {
        if (newIndex + 1 < policy.SustainedCheckMinIndex) return;

        double minutesSinceStart = (now - participant.StartedAt).TotalMinutes;
        if (minutesSinceStart <= 0) return;

        double sustainedWpm = (newIndex + 1) / policy.AverageWordLength / minutesSinceStart;
        if (sustainedWpm > policy.SustainedMaxWpm)
            throw new CheaterDetectedException("Sustained wpm");
    }
}
