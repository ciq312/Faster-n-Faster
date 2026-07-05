using FasterNFaster.Api.UseCases.Auth;

namespace FasterNFaster.Api.UseCases.Users.RegisterAnonymous;

public record RegisterAnonymousResult(string UserName, Guid UserId, TokenPair Tokens);
