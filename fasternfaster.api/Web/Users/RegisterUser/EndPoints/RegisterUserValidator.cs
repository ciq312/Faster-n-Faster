
using FastEndpoints;

public class RegisterUserValidator : Validator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Nick)
        .NotEmpty().WithErrorCode("Nick can't be empty null or empty string")
        .MaximumLength(30).WithMessage("Nick max length is 30");
        RuleFor(x => x.Login)
        .NotEmpty().WithErrorCode("Login can't be empty null or empty string")
        .MinimumLength(4).WithErrorCode("Login min length is 4")
        .MaximumLength(30).WithErrorCode("Login max length is 30");
        RuleFor(x => x.Password)
        .NotEmpty().WithErrorCode("Password can't be empty null or empty string")
        .MinimumLength(4).WithErrorCode("Password min length is 4")
        .MaximumLength(30).WithErrorCode("Password max length is 30");
    }
}