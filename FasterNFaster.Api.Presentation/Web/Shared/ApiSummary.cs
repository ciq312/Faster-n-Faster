using FastEndpoints;

namespace FasterNFaster.Api.Presentation.Web.Shared;

public abstract class ApiSummary<TEndpoint> : Summary<TEndpoint> where TEndpoint : IEndpoint
{
    protected ApiSummary() => Response<ApiErrorResponse>(500, "Unexpected server error");

    protected void ValidationFailed() =>
        Response<ApiErrorResponse>(400, "Validation failed; `errors` maps each field to its messages");

    protected void RateLimited() =>
        Response<ApiErrorResponse>(429, "Too many requests (`rate_limited`); retry after the `Retry-After` header");
}
