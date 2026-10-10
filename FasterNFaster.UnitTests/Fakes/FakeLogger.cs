using Microsoft.Extensions.Logging;

namespace FasterNFaster.Tests.Fakes;

public class FakeLogger<T> : ILogger<T>
{
    public List<LogLevel> Levels { get; } = new();
    public List<string> Messages { get; } = new();
    public List<Exception?> Exceptions { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Levels.Add(logLevel);
        Messages.Add(formatter(state, exception));
        Exceptions.Add(exception);
    }
}
