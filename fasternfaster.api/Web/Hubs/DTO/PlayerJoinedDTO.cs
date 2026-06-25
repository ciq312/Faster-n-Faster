namespace FasterNFaster.Api.Web.Hubs;

public partial class GameHub
{
    private record PlayerJoinedDTO(Guid PlayerId, string DisplayName);
}
