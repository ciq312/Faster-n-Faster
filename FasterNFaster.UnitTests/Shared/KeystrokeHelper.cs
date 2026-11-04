using FasterNFaster.Api.Domain.Entities.Keys;

namespace FasterNFaster.UnitTests.Shared;

public static class KeystrokeHelper
{
    public static Keystroke Backspace() => new Backspace();
    public static Keystroke CharTyped(char value) => new CharTyped(value);
    public static Keystroke[] Sequence(string input) => [.. input.Select(c => new CharTyped(c))];
}
