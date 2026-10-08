using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Exceptions;
using MediatR;
using FasterNFaster.Api.UseCases.Interfaces.Db;
using FasterNFaster.Api.Core.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Users.VerifyEmail;

public class VerifyEmailHandler(
    IUserRepository repo,
    IConfirmTokenRepository tokenRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<VerifyEmailCommand>
{
    public async Task Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var token = await tokenRepository.GetByValueAsync(command.Token) ?? throw new TokenNotFoundException();

        if (!token.IsValid() || token.Type != TokenType.EmailVerification) throw new TokenNotFoundException();

        var user = await repo.GetByIdAsync(token.UserId) ?? throw new UserNotFoundException(token.UserId);
        user.SetEmailVerified();
        repo.Update(user);
        await unitOfWork.SaveChangesAsync();

        await tokenRepository.Remove(token);
    }
}
