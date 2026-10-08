using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Exceptions;
using MediatR;
using FasterNFaster.Api.UseCases.Interfaces.Db;

namespace FasterNFaster.Api.UseCases.Users.VerifyEmail;

public class VerifyEmailHandler(
    IUserRepository repo,
    IConfirmTokenRepository tokenRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<VerifyEmailCommand>
{
    public async Task Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var token = await tokenRepository.GetByValueAsync(command.Token) ?? throw new TokenNotFoundException(command.Token);

        if (!token.IsValid()) throw new TokenNotFoundException(command.Token);

        var user = await repo.GetByIdAsync(token.UserId) ?? throw new UserNotFoundException(token.UserId);
        user.SetEmailVerified();
        repo.Update(user);
        await unitOfWork.SaveChangesAsync();

        await tokenRepository.Remove(token);
    }
}
