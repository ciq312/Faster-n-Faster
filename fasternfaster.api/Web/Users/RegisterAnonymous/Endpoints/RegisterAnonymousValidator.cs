using FastEndpoints;
using FasterNFaster.Api.Web.Users.RegisterAnonymous.Endpoints;
public class RegisterAnonymousValidator : Validator<RegisterAnonymousRequest>
{
    public RegisterAnonymousValidator()
    {
        RuleFor(x => x.Nick)
        .NotEmpty()
        .MaximumLength(10).WithMessage("Nick max length is 10");
    }
}