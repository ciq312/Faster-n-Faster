namespace FasterNFaster.Api.Core.Interfaces;

public interface ILobbyRepository : IRepository
{
    Task<bool> NameExistsAsync(string name);
    Task<bool> InviteCodeExistsAsync(string code);
    Task AddAsync(Entities.Lobby lobby);
    Task SaveChangesAsync();
}
