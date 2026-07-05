namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IBanRepository
{
    Task<bool> IsBannedAsync(Guid userId);
    Task BanAsync(Guid userId, string? reason);
}
