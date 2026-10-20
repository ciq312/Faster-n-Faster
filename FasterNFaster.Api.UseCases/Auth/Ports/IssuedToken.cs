namespace FasterNFaster.Api.UseCases.Auth.Ports;

public record IssuedToken(string Value, DateTimeOffset ExpiresAt, DateTimeOffset CreatedAt);
