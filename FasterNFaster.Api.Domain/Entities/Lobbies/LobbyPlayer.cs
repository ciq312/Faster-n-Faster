namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class LobbyPlayer : Entity<Guid>
{
    public string Nick { get; private set; }
    public int JoinOrder { get; private set; }
    public string Color { get; private set; }

    public LobbyPlayer(Guid userId, string nick, int joinOrder, string color)
    {
        Id = userId;
        Nick = nick;
        JoinOrder = joinOrder;
        Color = color;
    }
    public void ChangeColor(string newColor)
    {
        Color = newColor;
    }
}
