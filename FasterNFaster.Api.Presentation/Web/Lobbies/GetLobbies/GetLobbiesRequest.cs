namespace FasterNFaster.Api.Presentation.Web.Lobbies.GetLobbies;

public class GetLobbiesRequest
{
    public int Limit { get; set; } = 20;
    public string? Cursor { get; set; }
}
