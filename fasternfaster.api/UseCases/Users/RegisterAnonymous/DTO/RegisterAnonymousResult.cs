using FasterNFaster.Api.UseCases.Auth.Tokens;

namespace FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Results;

public record RegisterAnonymousResult(string UserName, Guid UserId, TokenPair Tokens);
