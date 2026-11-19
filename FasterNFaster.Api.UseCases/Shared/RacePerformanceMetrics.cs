using System.Diagnostics.Metrics;

namespace FasterNFaster.Api.UseCases.Shared;

public static class RacePerformanceMetrics
{
    public const string MeterName = "FasterNFaster.Races";
    private static readonly Meter meter = new(MeterName);
    private static readonly Counter<long> resultBatches = meter.CreateCounter<long>(
        "race.result.batches", "{batch}", "The number of race-result persistence batches by outcome.");

    public static Histogram<double> TickDuration { get; } = meter.CreateHistogram<double>("race.tick.duration", "ms");
    public static Histogram<double> PublishDuration { get; } = meter.CreateHistogram<double>("race.frame.publish.duration", "ms");
    public static Histogram<double> FrameAgeAtSend { get; } = meter.CreateHistogram<double>("race.frame.age_at_send", "ms");
    public static Histogram<double> BroadcastDuration { get; } = meter.CreateHistogram<double>("race.frame.broadcast.duration", "ms");
    public static Counter<long> ReplacedFrames { get; } = meter.CreateCounter<long>("race.frame.replaced", "{frame}");
    public static Histogram<double> GateWait { get; } = meter.CreateHistogram<double>("aggregate.gate.wait", "ms");
    public static Histogram<double> GateHold { get; } = meter.CreateHistogram<double>("aggregate.gate.hold", "ms");

    public static void RecordResultBatches(long count, bool persisted) =>
        resultBatches.Add(count, new KeyValuePair<string, object?>("outcome", persisted ? "persisted" : "failed"));
}
