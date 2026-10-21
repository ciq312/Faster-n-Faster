namespace FasterNFaster.Api.Presentation.Web.Shared;

public class ApiErrorResponse
{
    public string? Reason { get; set; }
    public string? Code { get; set; }

    public Dictionary<string, string[]>? Errors { get; set; }
}
