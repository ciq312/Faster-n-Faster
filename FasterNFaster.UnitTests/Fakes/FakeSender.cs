using MediatR;

namespace FasterNFaster.Tests.Fakes;

public class FakeSender : ISender
{
    public List<object> SentRequests { get; } = new();

    public Func<object, Task>? OnSend { get; set; }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        SentRequests.Add(request);
        return Task.FromResult<TResponse>(default!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        SentRequests.Add(request);
        return OnSend?.Invoke(request) ?? Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        SentRequests.Add(request);
        return Task.FromResult<object?>(null);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
