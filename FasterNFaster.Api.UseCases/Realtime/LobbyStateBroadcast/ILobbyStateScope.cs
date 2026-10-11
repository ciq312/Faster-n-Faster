namespace FasterNFaster.Api.UseCases.Realtime.LobbyStateBroadcast;

public interface ILobbyStateScope
{
    Task<T> Run<T>(Func<Task<T>> operation);
    Task Run(Func<Task> operation);
}
