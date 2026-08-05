using FasterNFaster.Api.Core.Interfaces.Events;
using MediatR;

namespace FasterNFaster.Api.UseCases.Events;

public record DomainEventNotification<T>(T Event) : INotification where T : IDomainEvent;
