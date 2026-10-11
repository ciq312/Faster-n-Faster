using MediatR;

namespace FasterNFaster.Api.UseCases.Users.RegisterUser;

public record RegisterUserCommand(string Nick, string Login, string Email, string Password) : IRequest<RegisterUserResult>;
