using FasterNFaster.Api.UseCases.Auth.Ports;

namespace FasterNFaster.Api.UseCases.Auth.RegisterAnonymous;

public record RegisterAnonymousResult(string UserName, Guid UserId, TokenPair Tokens);
