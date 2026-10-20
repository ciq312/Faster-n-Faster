using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.RegisterUser;

public record RegisterUserCommand(string Nick, string Login, string Email, string Password) : IRequest<RegisterUserResult>;
