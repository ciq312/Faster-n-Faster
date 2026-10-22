using TypedSignalR.Client;

namespace FasterNFaster.Api.Presentation.Web.Realtime;

[Hub]
public interface IGameHub
{
    Task ConnectToLobby(Guid lobbyId, string? inviteCode = null);
    Task RefreshLobby();
    Task StartRace();
    Task RefreshPassage();
    Task TransferHost(Guid targetPlayerId);
    Task KickPlayer(Guid targetPlayerId);
    Task ChangeColor(string color);
    Task UpdateRaceState(int index, int mistakes, string typed);
    Task LeaveLobby();
    Task<long> Ping(long clientSentMs);
}
