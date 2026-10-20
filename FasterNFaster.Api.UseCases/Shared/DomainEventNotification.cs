using FasterNFaster.Api.Domain.Interfaces.Events;
using MediatR;

namespace FasterNFaster.Api.UseCases.Shared;

public record DomainEventNotification<T>(T Event) : INotification where T : IDomainEvent;
