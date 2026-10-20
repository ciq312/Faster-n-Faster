using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.Logout;

public record LogoutCommand(string? RefreshToken, Guid? UserId) : IRequest;
