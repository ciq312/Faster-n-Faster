using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Results;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Queries;

public record GetLobbiesQuery : IRequest<GetLobbiesResult>;
