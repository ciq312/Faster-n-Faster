using FasterNFaster.Api.UseCases.Users.RefreshToken.DTO;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RefreshToken.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResult>;
