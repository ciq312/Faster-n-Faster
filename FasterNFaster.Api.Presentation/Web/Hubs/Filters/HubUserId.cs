using Microsoft.AspNetCore.SignalR;

namespace FasterNFaster.Api.Web.Hubs.Filters;

internal static class HubUserId
{
    public static bool TryGet(HubCallerContext context, out Guid userId)
    {
        userId = default;
        var sub = context.User?.FindFirst("sub")?.Value;
        return sub is not null && Guid.TryParse(sub, out userId);
    }
}
