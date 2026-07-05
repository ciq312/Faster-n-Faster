using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public record GetLobbiesQuery(string? InviteCode = null) : IRequest<GetLobbiesResult>;
