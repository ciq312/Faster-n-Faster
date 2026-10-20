using FasterNFaster.Api.UseCases.Auth.Ports;

namespace FasterNFaster.Api.UseCases.Auth.ExternalLogin;

public record ExternalLoginResult(TokenPair Tokens);
