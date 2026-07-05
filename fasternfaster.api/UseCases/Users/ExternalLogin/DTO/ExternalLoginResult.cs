using FasterNFaster.Api.UseCases.Auth.Tokens;

namespace FasterNFaster.Api.UseCases.Users.ExternalLogin;

public record ExternalLoginResult(TokenPair Tokens);
