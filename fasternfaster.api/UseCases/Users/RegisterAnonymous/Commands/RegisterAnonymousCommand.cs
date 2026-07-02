using FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Results;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RegisterAnonymous.Commands;

public record RegisterAnonymousCommand(string Nick) : IRequest<RegisterAnonymousResult>;
