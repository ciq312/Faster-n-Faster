namespace FasterNFaster.Api.UseCases.Exceptions;

public class DuplicateLoginException : Exception
{
    public string Login { get; }

    public DuplicateLoginException(string login)
        : base($"User with login '{login}' already exists")
    {
        Login = login;
    }
}
