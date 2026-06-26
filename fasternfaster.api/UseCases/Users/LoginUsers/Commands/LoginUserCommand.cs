using MediatR;

namespace FasterNFaster.Api.UseCases.Users.LoginUsers;

public record LoginUserCommand(string Login, string Password) : IRequest<LoginUserResult>;
