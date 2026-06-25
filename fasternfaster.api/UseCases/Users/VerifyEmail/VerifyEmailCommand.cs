using MediatR;

namespace FasterNFaster.Api.UseCases.Users.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest;
