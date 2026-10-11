using FasterNFaster.Api.UseCases.Auth.Ports;

namespace FasterNFaster.Api.UseCases.Auth.LoginUser;

public record LoginUserResult(Guid UserId, string Nick, TokenPair Tokens);
