using System.Diagnostics;
using OpenTelemetry;

namespace FasterNFaster.Api.Presentation.Web.Telemetry;

internal sealed class SafeTraceAttributesProcessor : BaseProcessor<Activity>
{
    private readonly ActivityListener exceptionListener = new()
    {
        ShouldListenTo = source => source.Name == "Npgsql",
        ExceptionRecorder = static (Activity _, Exception _, ref TagList tags) =>
        {
            tags.Add("exception.message", "Redacted");
            tags.Add("exception.stacktrace", "Redacted");
        }
    };

    public SafeTraceAttributesProcessor() => ActivitySource.AddActivityListener(exceptionListener);

    public override void OnEnd(Activity span)
    {
        span.SetTag("url.path", null);
        span.SetTag("url.query", null);
        span.SetTag("url.full", null);
        span.SetTag("user_agent.original", null);
        span.SetTag("db.query.text", null);
        span.SetTag("db.statement", null);
        span.SetTag("db.npgsql.data_source", null);

        if (span.Status == ActivityStatusCode.Error)
            span.SetStatus(ActivityStatusCode.Error);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            exceptionListener.Dispose();

        base.Dispose(disposing);
    }
}
