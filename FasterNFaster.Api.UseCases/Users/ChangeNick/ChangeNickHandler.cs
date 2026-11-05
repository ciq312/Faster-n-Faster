using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.ChangeNick;

public class ChangeNickHandler(
    IUserRepository userRepo,
    IUnitOfWork unitOfWork,
    ITokenService tokenService) : IRequestHandler<ChangeNickCommand, ChangeNickResult>
{
    public async Task<ChangeNickResult> Handle(ChangeNickCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepo.GetByIdAsync(command.UserId)
            ?? throw new UserNotFoundException(command.UserId);

        user.SetNick(command.Nick);

        userRepo.Update(user);
        await unitOfWork.SaveChangesAsync();

        var tokens = await tokenService.IssuePlayerTokens(user.Id, user.Nick);

        return new ChangeNickResult(tokens);
    }
}
