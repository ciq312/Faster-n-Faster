namespace FasterNFaster.Api.Presentation.Web.Shared;

public class ApiErrorResponse
{
    public string Reason { get; private set; }
    public string? Code { get; private set; }

    private readonly Dictionary<string, string[]>? errors;

    public IReadOnlyDictionary<string, string[]>? Errors => errors;

    public ApiErrorResponse(string reason, string code, Dictionary<string, string[]>? errors = null)
    {
        Reason = reason;
        Code = code;
        this.errors = errors;
    }

    public ApiErrorResponse(string reason, string code)
    {
        Reason = reason;
        Code = code;
    }
}
