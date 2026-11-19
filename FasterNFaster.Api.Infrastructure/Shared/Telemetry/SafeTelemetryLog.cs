using Npgsql;
using StackExchange.Redis;

namespace FasterNFaster.Api.Infrastructure.Shared.Telemetry;

public static class SafeTelemetryLog
{
    public const string OperationProperty = "TelemetryOperation";
    public const string HttpError = "http.error";
    public const string RaceTickFailed = "race.tick.failed";
    public const string RaceBroadcastFailed = "race.state.broadcast.failed";
    public const string RaceResultPersistenceFailed = "race.results.persist.failed";

    public static bool IsErrorOperation(string? operation) =>
        operation is HttpError or RaceTickFailed or RaceBroadcastFailed or RaceResultPersistenceFailed;

    public static void LogFailure(this ILogger logger, string operation, Exception exception) =>
        logger.LogError("{TelemetryOperation} ({ErrorKind})", operation, Classify(exception));

    public static string Classify(Exception exception)
    {
        for (var cause = exception; cause is not null; cause = cause.InnerException)
        {
            switch (cause)
            {
                case RedisTimeoutException:
                    return "redis_timeout";
                case NpgsqlException { InnerException: TimeoutException }:
                    return "postgresql_timeout";
            }
        }

        return exception switch
        {
            TimeoutException => "timeout",
            OperationCanceledException => "canceled",
            _ => "unexpected",
        };
    }
}
