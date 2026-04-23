namespace FasterNFaster.Api.UseCases.Exceptions;

public class DuplicateEmailException : Exception
{
    public string Email { get; private set; }

    public DuplicateEmailException(string email)
        : base($"User with email {email} already exists")
    {
        Email = email;
    }
}