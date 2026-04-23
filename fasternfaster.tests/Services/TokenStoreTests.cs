using FasterNFaster.Api.Web.Options.JwtOptions;
using FasterNFaster.Api.Web.Services.Implementations;
using Microsoft.Extensions.Options;

namespace FasterNFaster.Tests.Services;

public class TokenStoreTests
{
    private static TokenStore CreateStore()
    {
        var options = Options.Create(new JwtOptions
        {
            RefreshTokenLifetime = TimeSpan.FromHours(1)
        });
        return new TokenStore(options);
    }

    [Fact]
    public async Task StoreRefreshToken_ThenDeleteAllForUser_InvalidatesToken()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        var token = "refresh-A";

        await store.StoreRefreshToken(userId, token);
        await store.DeleteAllForUserAsync(userId);

        Assert.False(await store.IsRefreshTokenValid(token));
    }

    [Fact]
    public async Task DeleteAllForUser_OtherUsersTokensUnaffected()
    {
        var store = CreateStore();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var tokenA = "refresh-A";
        var tokenB = "refresh-B";

        await store.StoreRefreshToken(userA, tokenA);
        await store.StoreRefreshToken(userB, tokenB);

        await store.DeleteAllForUserAsync(userA);

        Assert.False(await store.IsRefreshTokenValid(tokenA));
        Assert.True(await store.IsRefreshTokenValid(tokenB));
    }

    [Fact]
    public async Task DeleteAllForUser_NoEntry_NoThrow()
    {
        var store = CreateStore();
        var unknownUser = Guid.NewGuid();

        await store.DeleteAllForUserAsync(unknownUser);
    }

    [Fact]
    public async Task DeleteAllForUser_ClearsReverseLookup_SoUserIdByTokenReturnsEmpty()
    {
        var store = CreateStore();
        var userId = Guid.NewGuid();
        var token = "refresh-A";

        await store.StoreRefreshToken(userId, token);
        await store.DeleteAllForUserAsync(userId);

        Assert.Equal(Guid.Empty, await store.GetUserIdByTokenAsync(token));
    }
}
