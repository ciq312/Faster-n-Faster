using Tapper;

namespace FasterNFaster.Api.Presentation.Web.Realtime;

[TranspilationSource]
public sealed record RaceProgressDTO(int LastValidIndex, string WrongTail, int Mistakes);
