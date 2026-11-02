namespace FasterNFaster.Api.Domain.Entities.Keys;

public sealed record Backspace(int DelayMs) : Keystroke(DelayMs);
