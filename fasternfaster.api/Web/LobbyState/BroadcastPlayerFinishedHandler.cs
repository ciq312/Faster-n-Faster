using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.UseCases.Events;
using FasterNFaster.Api.Web.Hubs;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using static FasterNFaster.Api.Web.Hubs.GameHubConstants;

namespace FasterNFaster.Api.Web.LobbyState;

public class BroadcastPlayerFinishedHandler(IHubContext<GameHub> hub)
    : INotificationHandler<DomainEventNotification<PlayerFinishedEvent>>
{
    public async Task Handle(DomainEventNotification<PlayerFinishedEvent> notification, CancellationToken cancellationToken)
    {
        var e = notification.Event;
        await hub.Clients.Group(LobbyGroup(e.LobbyId)).SendAsync(Methods.PlayerFinished,
            new PlayerFinishedDTO(e.Nick, e.PlayerId, e.FinishPosition, e.Wpm, e.Accuracy));
    }
}

public record PlayerFinishedDTO(string Nick, Guid PlayerId, int? FinishPosition, double Wpm, double Accuracy);
