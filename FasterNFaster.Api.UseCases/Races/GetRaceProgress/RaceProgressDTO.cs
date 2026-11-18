using Tapper;

namespace FasterNFaster.Api.UseCases.Races.GetRaceProgress;

[TranspilationSource]
public sealed record RaceProgressDTO(int LastValidIndex, string WrongTail, int Mistakes);
