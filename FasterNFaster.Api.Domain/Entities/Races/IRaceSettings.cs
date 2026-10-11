using System.Text.Json.Serialization;
using static FasterNFaster.Api.Domain.Entities.Races.WordRace;

namespace FasterNFaster.Api.Domain.Entities.Races;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(WordRaceSettings), "word")]
public interface IRaceSettings
{
    string RaceType { get; }
}
