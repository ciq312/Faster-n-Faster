using System.Buffers.Text;
using System.Text.Json;
using FasterNFaster.Api.UseCases.Lobbies.Exceptions;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public sealed record LobbyCursor(DateTimeOffset CreatedAt, Guid Id)
{
    private static readonly JsonSerializerOptions Options = new() { RespectRequiredConstructorParameters = true };

    public static LobbyCursor After(LobbyListItem item) => new(item.CreatedAt, item.Id);

    public string Encode() => Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(this, Options));

    public static LobbyCursor Decode(string cursor)
    {
        try
        {
            return JsonSerializer.Deserialize<LobbyCursor>(Base64Url.DecodeFromChars(cursor), Options)
                ?? throw new InvalidLobbyCursorException();
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new InvalidLobbyCursorException();
        }
    }

    public bool Precedes(LobbyListItem item) =>
        item.CreatedAt < CreatedAt || (item.CreatedAt == CreatedAt && item.Id.CompareTo(Id) > 0);
}
