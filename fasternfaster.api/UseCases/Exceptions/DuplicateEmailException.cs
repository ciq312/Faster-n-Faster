using FasterNFaster.Api.Core.Exceptions;

namespace FasterNFaster.Api.UseCases.Exceptions;

public class DuplicateEmailException : DomainException
{
    public string Email { get; private set; }

    public DuplicateEmailException(string email)
        : base($"User with email {email} already exists")
    {
        Email = email;
    }
}