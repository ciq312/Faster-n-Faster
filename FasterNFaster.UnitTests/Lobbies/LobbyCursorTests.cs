using FasterNFaster.Api.UseCases.Lobbies.Exceptions;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies;
using Shouldly;

namespace FasterNFaster.UnitTests.Lobbies;

public class LobbyCursorTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 21, 18, 30, 0, TimeSpan.Zero);
    private static readonly Guid MiddleId = Guid.Parse("00000000-0000-0000-0000-000000000005");

    [Fact]
    public void EncodeThenDecode_RoundTripsExactly()
    {
        var cursor = new LobbyCursor(CreatedAt.AddTicks(1234), Guid.NewGuid());

        LobbyCursor.Decode(cursor.Encode()).ShouldBe(cursor);
    }

    [Theory]
    [InlineData("%%%")]
    [InlineData("bm90IGpzb24")]
    [InlineData("e30")]
    [InlineData("bnVsbA")]
    public void MalformedCursor_ThrowsInvalidLobbyCursor(string cursor)
    {
        Should.Throw<InvalidLobbyCursorException>(() => LobbyCursor.Decode(cursor));
    }

    [Fact]
    public void OlderItem_ComesAfterCursor()
    {
        Cursor().Precedes(Item(CreatedAt.AddSeconds(-1), MiddleId)).ShouldBeTrue();
    }

    [Fact]
    public void SameTimeHigherId_ComesAfterCursor()
    {
        Cursor().Precedes(Item(CreatedAt, Guid.Parse("00000000-0000-0000-0000-000000000009"))).ShouldBeTrue();
    }

    [Fact]
    public void SameTimeLowerId_DoesNotComeAfterCursor()
    {
        Cursor().Precedes(Item(CreatedAt, Guid.Parse("00000000-0000-0000-0000-000000000001"))).ShouldBeFalse();
    }

    [Fact]
    public void NewerItem_DoesNotComeAfterCursor()
    {
        Cursor().Precedes(Item(CreatedAt.AddSeconds(1), MiddleId)).ShouldBeFalse();
    }

    private static LobbyCursor Cursor() => new(CreatedAt, MiddleId);

    private static LobbyListItem Item(DateTimeOffset createdAt, Guid id) =>
        new(id, "Lobby", false, false, 1, 6, createdAt);
}
