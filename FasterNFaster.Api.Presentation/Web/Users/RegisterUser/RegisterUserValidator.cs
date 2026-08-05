
using FastEndpoints;

namespace FasterNFaster.Api.Web.Users.RegisterUser;

public class RegisterUserValidator : Validator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Nick)
        .NotEmpty().WithMessage("Nick can't be empty")
        .MaximumLength(10).WithMessage("Nick max length is 10");
        RuleFor(x => x.Login)
        .NotEmpty().WithMessage("Login can't be empty")
        .MaximumLength(10).WithMessage("Login max length is 10");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email can't be empty")
            .EmailAddress().WithMessage("Only email addresses accepted");
        RuleFor(x => x.Password)
        .NotEmpty().WithMessage("Password can't be empty")
        .MinimumLength(4).WithMessage("Password min length is 4")
        .MaximumLength(30).WithMessage("Password max length is 30");
    }
}