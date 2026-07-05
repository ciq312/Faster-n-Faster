using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RegisterAnonymous;

public record RegisterAnonymousCommand(string Nick) : IRequest<RegisterAnonymousResult>;
