using System.Text.Json.Serialization;
using static FasterNFaster.Api.Core.Entities.Races.WordRace;

namespace FasterNFaster.Api.Core.Entities.Races
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(WordRaceSettings), "word")]
    public interface IRaceSettings
    {
        string RaceType { get; }
    }
}
