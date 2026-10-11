using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace FasterNFaster.IntegrationTests;

public class RedisConfirmTokenRepositoryTests(NoRateLimitApplicationFactory<Program> fixture) : IntegrationTestBase(fixture), IClassFixture<NoRateLimitApplicationFactory<Program>>
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan ShortLifetime = TimeSpan.FromMilliseconds(200);

    private IConfirmTokenRepository repo = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        repo = App.Services.GetRequiredService<IConfirmTokenRepository>();
    }

    [Fact]
    public async Task AddedToken_FoundByValueWithAllFields()
    {
        var token = NewToken(Guid.NewGuid(), TokenType.PasswordReset, Lifetime);
        await repo.Add(token);

        var stored = await repo.GetByValueAsync(token.Value);

        Assert.NotNull(stored);
        Assert.Equal(token.UserId, stored.UserId);
        Assert.Equal(token.Value, stored.Value);
        Assert.Equal(token.Type, stored.Type);
        Assert.Equal(token.CreatedAt, stored.CreatedAt);
        Assert.Equal(token.ExpiresAt, stored.ExpiresAt);
    }

    [Fact]
    public async Task UnknownValue_NotFound()
    {
        Assert.Null(await repo.GetByValueAsync("unknown"));
    }

    [Fact]
    public async Task AddedToken_IsLatestForUser()
    {
        var token = NewToken(Guid.NewGuid(), TokenType.EmailVerification, Lifetime);
        await repo.Add(token);

        var latest = await repo.GetLatestForUserAsync(token.UserId, TokenType.EmailVerification);

        Assert.NotNull(latest);
        Assert.Equal(token.Value, latest.Value);
    }

    [Fact]
    public async Task UserWithoutToken_HasNoLatest()
    {
        Assert.Null(await repo.GetLatestForUserAsync(Guid.NewGuid(), TokenType.EmailVerification));
    }

    [Fact]
    public async Task SecondTokenOfSameType_ReplacesFirst()
    {
        var userId = Guid.NewGuid();
        var first = NewToken(userId, TokenType.EmailVerification, Lifetime);
        var second = NewToken(userId, TokenType.EmailVerification, Lifetime);
        await repo.Add(first);
        await repo.Add(second);

        var latest = await repo.GetLatestForUserAsync(userId, TokenType.EmailVerification);

        Assert.Null(await repo.GetByValueAsync(first.Value));
        Assert.NotNull(await repo.GetByValueAsync(second.Value));
        Assert.NotNull(latest);
        Assert.Equal(second.Value, latest.Value);
    }

    [Fact]
    public async Task TokensOfDifferentTypes_KeptIndependently()
    {
        var userId = Guid.NewGuid();
        var verification = NewToken(userId, TokenType.EmailVerification, Lifetime);
        var reset = NewToken(userId, TokenType.PasswordReset, Lifetime);
        await repo.Add(verification);
        await repo.Add(reset);

        var latestVerification = await repo.GetLatestForUserAsync(userId, TokenType.EmailVerification);
        var latestReset = await repo.GetLatestForUserAsync(userId, TokenType.PasswordReset);

        Assert.NotNull(latestVerification);
        Assert.NotNull(latestReset);
        Assert.Equal(verification.Value, latestVerification.Value);
        Assert.Equal(reset.Value, latestReset.Value);
        Assert.NotNull(await repo.GetByValueAsync(verification.Value));
        Assert.NotNull(await repo.GetByValueAsync(reset.Value));
    }

    [Fact]
    public async Task AlreadyExpiredToken_NotStored()
    {
        var token = NewToken(Guid.NewGuid(), TokenType.EmailVerification, TimeSpan.FromMinutes(-1));

        await repo.Add(token);

        Assert.Null(await repo.GetByValueAsync(token.Value));
        Assert.Null(await repo.GetLatestForUserAsync(token.UserId, TokenType.EmailVerification));
    }

    [Fact]
    public async Task TokenPastLifetime_NotFound()
    {
        var token = NewToken(Guid.NewGuid(), TokenType.EmailVerification, ShortLifetime);
        await repo.Add(token);

        await Task.Delay(ShortLifetime * 2);

        Assert.Null(await repo.GetByValueAsync(token.Value));
        Assert.Null(await repo.GetLatestForUserAsync(token.UserId, TokenType.EmailVerification));
    }

    [Fact]
    public async Task RemovedLatestToken_NotFoundByValueOrAsLatest()
    {
        var token = NewToken(Guid.NewGuid(), TokenType.PasswordReset, Lifetime);
        await repo.Add(token);

        await repo.Remove(token);

        Assert.Null(await repo.GetByValueAsync(token.Value));
        Assert.Null(await repo.GetLatestForUserAsync(token.UserId, TokenType.PasswordReset));
    }

    [Fact]
    public async Task RemovingSupersededToken_KeepsNewerAsLatest()
    {
        var userId = Guid.NewGuid();
        var first = NewToken(userId, TokenType.PasswordReset, Lifetime);
        var second = NewToken(userId, TokenType.PasswordReset, Lifetime);
        await repo.Add(first);
        await repo.Add(second);

        await repo.Remove(first);

        var latest = await repo.GetLatestForUserAsync(userId, TokenType.PasswordReset);
        Assert.NotNull(latest);
        Assert.Equal(second.Value, latest.Value);
    }

    private static Token NewToken(Guid userId, TokenType type, TimeSpan lifetime) => new()
    {
        UserId = userId,
        Value = Guid.NewGuid().ToString("N"),
        Type = type,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow + lifetime
    };
}
