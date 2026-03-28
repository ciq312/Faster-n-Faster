namespace FasterNFaster.Api.Core.Entities.Lobbies.Colors;

public static class PlayerColors
{
    public static readonly string[] Palette =
    [
        "#FF6B6B", "#63f800", "#19d5ff", "#ebff0a", "#0059ff"
        ,"#ff0894", "#ff680a"
    ];

    public static string Get(int index) =>
        Palette[index % Palette.Length];

    public static string GetFirstAvailableFromPalette(IEnumerable<string> takenColors)
    {
        var taken = new HashSet<string>(takenColors);
        return Palette.First(c => !taken.Contains(c));
    }
}
