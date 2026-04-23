namespace FasterNFaster.Api.Web.Options.App;

public class AppOptions
{
    public string? FrontendUrl { get; private set; } = "http://localhost:3000";
    public string? BackendUrl { get; private set; }
}