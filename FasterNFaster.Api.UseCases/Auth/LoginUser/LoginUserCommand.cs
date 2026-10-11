using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.LoginUser;

public record LoginUserCommand(string Login, string Password) : IRequest<LoginUserResult>;
