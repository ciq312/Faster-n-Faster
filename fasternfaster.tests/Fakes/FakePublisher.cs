using MediatR;

namespace FasterNFaster.Tests.Fakes;

public class FakePublisher : IPublisher
{
    public List<INotification> Published { get; } = new();

    public Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        if (notification is INotification n) Published.Add(n);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        Published.Add(notification);
        return Task.CompletedTask;
    }
}
