namespace FasterNFaster.Api.UseCases.Auth.Ports;

public record IssuedToken(string Value, DateTime ExpiresAt, DateTime CreatedAt);
