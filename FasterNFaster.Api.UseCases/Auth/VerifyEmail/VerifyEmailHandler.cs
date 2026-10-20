using FasterNFaster.Api.Domain.Entities.Auth;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Exceptions;
using FasterNFaster.Api.UseCases.Auth.Ports;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Auth.VerifyEmail;

public class VerifyEmailHandler(
    IUserRepository repo,
    IConfirmTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<VerifyEmailCommand>
{
    public async Task Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var token = await tokenRepository.GetByValueAsync(command.Token) ?? throw new TokenNotFoundException();

        if (!token.IsValid(timeProvider.GetUtcNow()) || token.Type != TokenType.EmailVerification) throw new TokenNotFoundException();

        var user = await repo.GetByIdAsync(token.UserId) ?? throw new UserNotFoundException(token.UserId);
        user.SetEmailVerified();
        repo.Update(user);
        await unitOfWork.SaveChangesAsync();

        await tokenRepository.Remove(token);
    }
}
