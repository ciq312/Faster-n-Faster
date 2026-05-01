using FasterNFaster.Api.Core.Exceptions;

namespace FasterNFaster.Api.UseCases.Exceptions;

public class DuplicateLoginException : DomainException
{
    public string Login { get; }

    public DuplicateLoginException(string login)
        : base($"User with login '{login}' already exists")
    {
        Login = login;
    }
}
