using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.ExternalLogin;

public record ExternalLoginCommand(string Provider, string Subject, string Email, bool EmailVerified) : IRequest<ExternalLoginResult>;
