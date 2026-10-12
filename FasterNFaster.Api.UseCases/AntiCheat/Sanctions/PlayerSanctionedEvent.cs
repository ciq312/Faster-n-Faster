using MediatR;

namespace FasterNFaster.Api.UseCases.AntiCheat.Sanctions;

public record PlayerSanctionedEvent(Guid PlayerId) : INotification;
