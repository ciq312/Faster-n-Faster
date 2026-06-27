using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Api.Core.Exceptions;
using FasterNFaster.Api.UseCases.Exceptions;
using MediatR;

namespace FasterNFaster.Api.UseCases.Users.VerifyEmail;

public class VerifyEmailHandler(IUserRepository repo, ITokenRepository tokenRepository) : IRequestHandler<VerifyEmailCommand>
{
    private readonly IUserRepository repo = repo;
    private readonly ITokenRepository tokenRepository = tokenRepository;

    public async Task Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        Token token = await tokenRepository.GetByValueAsync(command.Token) ?? throw new TokenNotFoundException(command.Token);

        if (!token.TryVerify()) throw new InvalidOperationException("Can't verify provided token");

        User user = await repo.GetByIdAsync(token.UserId) ?? throw new UserNotFoundException(token.UserId);
        user.SetEmailVerified();

        await tokenRepository.Remove(token);
        await tokenRepository.SaveChangesAsync();
    }
}
