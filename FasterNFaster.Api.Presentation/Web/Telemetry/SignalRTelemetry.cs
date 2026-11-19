using System.Diagnostics;

namespace FasterNFaster.Api.Presentation.Web.Telemetry;

internal static class SignalRTelemetry
{
    public const string SourceName = "FasterNFaster.SignalR";
    public const string OperationProperty = "TelemetryOperation";
    public const string Connect = "signalr.connect";
    public const string StartRace = "signalr.start_race";

    public static readonly ActivitySource Source = new(SourceName);
}
