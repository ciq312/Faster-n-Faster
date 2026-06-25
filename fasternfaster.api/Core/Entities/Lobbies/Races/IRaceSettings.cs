using System.Text.Json.Serialization;
using static FasterNFaster.Api.Core.Entities.Lobbies.Races.WordRace;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Races
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(WordRaceSettings), "word")]
    public interface IRaceSettings
    {
        string RaceType { get; }
    }
}
