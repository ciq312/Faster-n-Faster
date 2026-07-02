using FasterNFaster.Api.UseCases.Auth.Tokens;

namespace FasterNFaster.Api.UseCases.Users.LoginUsers;

public record LoginUserResult(Guid UserId, string Nick, TokenPair Tokens);
