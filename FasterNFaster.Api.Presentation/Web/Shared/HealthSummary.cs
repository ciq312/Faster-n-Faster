namespace FasterNFaster.Api.Presentation.Web.Shared;

public class HealthSummary : ApiSummary<HealthEndpoint>
{
    public HealthSummary()
    {
        Summary = "Check that the API is up";
        Response<HealthResponse>(200, "The API is up", example: new HealthResponse { Status = "healthy" });
    }
}
