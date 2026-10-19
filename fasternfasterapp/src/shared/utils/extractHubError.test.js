import { extractHubError } from "./extractHubError";
import { describe, it, expect } from "vitest";

describe("extractHubError", () => {
  it("gives fallback message if not familiar mesasge", () => {
    const msg = extractHubError({
      message: "random message",
    });

    expect("Something went wrong, try again", msg);
  });

  it("shows error when HubException happens", () => {
    const msgToShow = "player is not in lobby";
    const msg = extractHubError({
      message: `HubException: ${msgToShow}`,
    });
    expect(msgToShow, msg);
  });

  it;
});
