using FastEndpoints;
using FasterNFaster.Api.Web.Users.RegisterAnonymous;
public class RegisterAnonymousValidator : Validator<RegisterAnonymousRequest>
{
    public RegisterAnonymousValidator()
    {
        RuleFor(x => x.Nick)
        .NotEmpty()
        .MaximumLength(10).WithMessage("Nick max length is 10");
    }
}