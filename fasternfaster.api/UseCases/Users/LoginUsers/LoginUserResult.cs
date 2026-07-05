using FasterNFaster.Api.UseCases.Auth;

namespace FasterNFaster.Api.UseCases.Users.LoginUsers;

public record LoginUserResult(Guid UserId, string Nick, TokenPair Tokens);
