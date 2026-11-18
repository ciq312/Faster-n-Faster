using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.UseCases.Auth.Exceptions;

public class EmailNotVerifiedException(string email) : ForbiddenException("Email verification required")
{
    public string Email { get; } = email;

    public override string Code => ErrorCodes.EmailNotVerified;

    public override IReadOnlyDictionary<string, string[]> Extensions => new Dictionary<string, string[]> { ["email"] = new[] { Email } };
}
