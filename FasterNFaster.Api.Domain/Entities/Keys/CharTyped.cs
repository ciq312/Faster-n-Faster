namespace FasterNFaster.Api.Domain.Entities.Keys;

public sealed record CharTyped(char Value, int DelayMs) : Keystroke(DelayMs);