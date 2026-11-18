using System.Diagnostics.Metrics;

namespace FasterNFaster.Api.UseCases.Shared;

public static class RacePerformanceMetrics
{
    private static readonly Meter meter = new("FasterNFaster.Races");

    public static Histogram<double> TickDuration { get; } = meter.CreateHistogram<double>("race.tick.duration", "ms");
    public static Histogram<double> PublishDuration { get; } = meter.CreateHistogram<double>("race.frame.publish.duration", "ms");
    public static Histogram<double> FrameAgeAtSend { get; } = meter.CreateHistogram<double>("race.frame.age_at_send", "ms");
    public static Histogram<double> BroadcastDuration { get; } = meter.CreateHistogram<double>("race.frame.broadcast.duration", "ms");
    public static Counter<long> ReplacedFrames { get; } = meter.CreateCounter<long>("race.frame.replaced", "{frame}");
    public static Histogram<double> GateWait { get; } = meter.CreateHistogram<double>("aggregate.gate.wait", "ms");
    public static Histogram<double> GateHold { get; } = meter.CreateHistogram<double>("aggregate.gate.hold", "ms");
}
