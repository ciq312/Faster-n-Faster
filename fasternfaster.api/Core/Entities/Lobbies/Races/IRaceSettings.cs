using System.Text.Json.Serialization;
using static FasterNFaster.Api.Core.Entities.Lobbies.Races.WordRace;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(WordRaceSettings), "word")]
public interface IRaceSettings
{
}