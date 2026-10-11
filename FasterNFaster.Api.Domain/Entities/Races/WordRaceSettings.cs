namespace FasterNFaster.Api.Domain.Entities.Races;

public partial class WordRace
{
    public record WordRaceSettings(string Passage, int WordCount) : IRaceSettings
    {
        public string RaceType => "WordRace";
    }
}
