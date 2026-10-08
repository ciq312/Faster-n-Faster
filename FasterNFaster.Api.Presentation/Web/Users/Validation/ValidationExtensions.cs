namespace FasterNFaster.Api.Web.Users.Validation;

public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Password can't be empty")
            .MinimumLength(8).WithMessage("Password min length is {MinLength}")
            .MaximumLength(64).WithMessage("Password max length is {MaxLength}");
    }
}
