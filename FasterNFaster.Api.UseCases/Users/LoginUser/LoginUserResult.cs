using FasterNFaster.Api.UseCases.Auth;

namespace FasterNFaster.Api.UseCases.Users.LoginUser;

public record LoginUserResult(Guid UserId, string Nick, TokenPair Tokens);
