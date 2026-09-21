namespace FasterNFaster.Api.UseCases.Auth;

public record IssuedToken(string Value, DateTime ExpiresAt, DateTime CreatedAt);