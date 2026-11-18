using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public record GetLobbiesQuery(int Limit, string? Cursor = null) : IRequest<GetLobbiesResult>;
