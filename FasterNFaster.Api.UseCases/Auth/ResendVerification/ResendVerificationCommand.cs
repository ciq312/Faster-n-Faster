using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.ResendVerification;

public record ResendVerificationCommand(string Email) : IRequest;
