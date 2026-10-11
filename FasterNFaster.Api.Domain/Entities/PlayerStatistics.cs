using FasterNFaster.Api.Domain.Entities.Races;

namespace FasterNFaster.Api.Domain.Entities;

public class PlayerStatistics : Entity<Guid>
{
    public PlayerStatistics(Guid id)
    {
        Id = id;
    }
    public User User { get; private set; } = null!;

    public int Wins { get; private set; }
    public int RacesTyped { get; private set; }
    public float BestWPM { get; private set; }
    public float SumWPM { get; private set; }
    public float AvgWPM => RacesTyped == 0 ? 0 : SumWPM / RacesTyped;

    public float BestAccuracy { get; private set; }
    public float SumAccuracy { get; private set; }
    public float AvgAccuracy => RacesTyped == 0 ? 0 : SumAccuracy / RacesTyped;

    public int WordsTyped { get; private set; }

    public void RegisterRace(RaceParticipantResult result)
    {
        RacesTyped++;

        if (result.FinishPosition == 1) Wins++;

        BestWPM = Math.Max(BestWPM, result.WPM);
        SumWPM += result.WPM;

        BestAccuracy = Math.Max(BestAccuracy, result.Accuracy);
        SumAccuracy += result.Accuracy;

        WordsTyped += result.WordsTyped;
    }
}
