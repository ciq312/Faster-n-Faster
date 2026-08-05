namespace FasterNFaster.Api.Core.Entities.Lobbies.Colors;

public static class PlayerColors
{
    public static readonly string[] Palette =
    [
        "#FF6B6B", "#63f800", "#19d5ff", "#ebff0a", "#0059ff"
        ,"#ff0894", "#ff680a", "#00ff9c", "#ffde59", "#8c00ff", "#ff00c8", "#ff9a00", "#00ffea", "#fff500", "#4b0082"
    ];

    public static string Get(int index) =>
        Palette[index % Palette.Length];

    public static string GetFirstAvailableFromPalette(IEnumerable<string> takenColors)
    {
        var taken = new HashSet<string>(takenColors);
        return Palette.First(c => !taken.Contains(c));
    }
}
