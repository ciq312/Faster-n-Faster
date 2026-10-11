using MediatR;

namespace FasterNFaster.Api.UseCases.Users.LoginUser;

public record LoginUserCommand(string Login, string Password) : IRequest<LoginUserResult>;
