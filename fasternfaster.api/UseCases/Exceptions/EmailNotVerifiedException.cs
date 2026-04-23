namespace FasterNFaster.Api.UseCases.Exceptions;

public class EmailNotVerifiedException(string email) : Exception("Email verification required")
{
    public string Email { get; } = email;
}
