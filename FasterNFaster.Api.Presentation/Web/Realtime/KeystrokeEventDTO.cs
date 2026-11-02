using FasterNFaster.Api.Domain.Entities.Keys;
using Microsoft.AspNetCore.SignalR;
using Tapper;

namespace FasterNFaster.Api.Presentation.Web.Realtime;

[TranspilationSource]
public sealed record KeystrokeEventDTO(string Key, int DelayMs)
{
    public Keystroke ToKeystroke() => Key switch
    {
        "Backspace" => new Backspace(DelayMs),
        [var c] => new CharTyped(c, DelayMs),
        _ => throw new HubException($"Invalid key: {Key}")
    };
}
