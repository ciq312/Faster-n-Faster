using FasterNFaster.Api.Domain.Exceptions;

namespace FasterNFaster.Api.UseCases.Exceptions;

public class EmailNotVerifiedException(string email) : ForbiddenException("Email verification required")
{
    public string Email { get; } = email;

    public override string Code => ErrorCodes.EmailNotVerified;

    public override IReadOnlyDictionary<string, object?> Extensions => new Dictionary<string, object?> { ["email"] = Email };
}
