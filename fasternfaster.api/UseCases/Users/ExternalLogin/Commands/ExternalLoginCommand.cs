using FasterNFaster.Api.UseCases.Users.ExternalLogin.DTO;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.ExternalLogin.Commands;

public record ExternalLoginCommand(string Provider, string Subject, string Email, string Name, bool EmailVerified) : IRequest<ExternalLoginResult>;
