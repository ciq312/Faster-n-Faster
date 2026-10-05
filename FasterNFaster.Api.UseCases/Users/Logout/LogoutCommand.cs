using MediatR;

namespace FasterNFaster.Api.UseCases.Users.Logout;

public record LogoutCommand(string? RefreshToken, Guid? UserId) : IRequest;
