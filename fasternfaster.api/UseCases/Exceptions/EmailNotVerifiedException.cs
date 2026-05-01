using FasterNFaster.Api.Core.Exceptions;

namespace FasterNFaster.Api.UseCases.Exceptions;

public class EmailNotVerifiedException(string email) : DomainException("Email verification required")
{
    public string Email { get; } = email;
}
