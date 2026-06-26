using MediatR;

namespace FasterNFaster.Api.UseCases.Users.ResendVerification;

public record ResendVerificationCommand(string Email) : IRequest;
