// ASP.NET Core SignalR sends HubException messages as "<generic prefix> HubException: <message>".
const HUB_EXCEPTION_MARKER = "HubException: ";
const FALLBACK_MESSAGE = "Something went wrong, try again";

export function extractHubError(error) {
  const message = typeof error?.message === "string" ? error.message : "";
  const markerIndex = message.indexOf(HUB_EXCEPTION_MARKER);
  if (markerIndex === -1) return FALLBACK_MESSAGE;
  return (
    message.slice(markerIndex + HUB_EXCEPTION_MARKER.length) || FALLBACK_MESSAGE
  );
}
