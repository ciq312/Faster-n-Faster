using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.RegisterAnonymous;

public record RegisterAnonymousCommand(string Nick) : IRequest<RegisterAnonymousResult>;
