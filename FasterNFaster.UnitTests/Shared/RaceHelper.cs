using FasterNFaster.Api.Domain.Entities.Keys;

namespace FasterNFaster.UnitTests.Shared;

public static class RaceHelper
{
    public static Keystroke Backspace(int delayMs = 0) => new Backspace(delayMs);
    public static Keystroke CharTyped(char value, int delayMs = 0) => new CharTyped(value, delayMs);
    public static Keystroke[] Sequence(string input, int delayMs = 0) => [.. input.Select((c, i) => new CharTyped(c, i * delayMs))];
}