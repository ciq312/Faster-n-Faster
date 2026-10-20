using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.RequestPasswordReset;

public record RequestPasswordResetCommand(string Email) : IRequest;
