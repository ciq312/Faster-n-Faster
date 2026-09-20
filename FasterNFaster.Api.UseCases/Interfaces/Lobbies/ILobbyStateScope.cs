namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyStateScope
{
    Task<T> Run<T>(Func<Task<T>> operation);
    Task Run(Func<Task> operation);
}
