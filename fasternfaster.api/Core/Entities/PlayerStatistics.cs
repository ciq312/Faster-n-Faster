using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.Core.Entities;

public class PlayerStatistics(Guid id)
{
    public Guid Id { get; set; } = id;
    public User User { get; private set; } = null!;

    public int Wins { get; private set; }
    public int RacesTyped { get; private set; }
    public float BestWPM { get; private set; }
    public float SumWPM { get; private set; }
    public float AvgWPM { get; private set; }

    public float BestAccuracy { get; private set; }
    public float SumAccuracy { get; private set; }
    public float AvgAccuracy { get; private set; }

    public int WordsTyped { get; private set; }

    public void RegisterRace(RaceParticipantResult result)
    {
        RacesTyped++;

        if (result.FinishPosition == 1) Wins++;

        BestWPM = Math.Max(BestWPM, result.WPM);
        SumWPM += result.WPM;
        AvgWPM = SumWPM / RacesTyped;

        BestAccuracy = Math.Max(BestAccuracy, result.Accuracy);
        SumAccuracy += result.Accuracy;
        AvgAccuracy = SumAccuracy / RacesTyped;

        WordsTyped += result.WordsTyped;
    }
}
