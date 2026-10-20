using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest;
