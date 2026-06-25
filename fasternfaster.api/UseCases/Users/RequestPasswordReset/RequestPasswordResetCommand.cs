using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email) : IRequest;
