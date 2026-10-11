using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.UseCases.Exceptions;

public class DuplicateEmailException : ConflictException
{
    public string Email { get; private set; }

    public DuplicateEmailException(string email)
        : base($"User with email {email} already exists")
    {
        Email = email;
    }
}
