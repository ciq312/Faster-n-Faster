import { extractHubError } from "./extractHubError";
import { describe, it, expect } from "vitest";

const FALLBACK_MESSAGE = "Something went wrong, try again";

describe("extractHubError", () => {
  it("gives fallback message if not familiar message", () => {
    const msg = extractHubError({
      message: "random message",
    });

    expect(msg).toBe(FALLBACK_MESSAGE);
  });

  it("shows error when HubException happens", () => {
    const msgToShow = "player is not in lobby";
    const msg = extractHubError({
      message: `HubException: ${msgToShow}`,
    });

    expect(msg).toBe(msgToShow);
  });

  it("strips the generic SignalR prefix before the marker", () => {
    const msg = extractHubError({
      message:
        "An unexpected error occurred invoking 'X' on the server. HubException: Lobby is full",
    });

    expect(msg).toBe("Lobby is full");
  });

  it("gives fallback message if the text after the marker is empty", () => {
    const msg = extractHubError({ message: "HubException: " });

    expect(msg).toBe(FALLBACK_MESSAGE);
  });

  it("gives fallback message for input without a string message", () => {
    expect(extractHubError(undefined)).toBe(FALLBACK_MESSAGE);
    expect(extractHubError({ message: 42 })).toBe(FALLBACK_MESSAGE);
  });
});
