namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface IBanService
{
    Task<bool> IsBannedAsync(Guid userId);
    Task BanAsync(Guid userId, string? reason);
}
