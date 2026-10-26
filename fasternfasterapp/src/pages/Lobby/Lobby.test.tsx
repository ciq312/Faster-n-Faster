import { act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { fakeGameHub } from "../../test/fakeGameHub";
import { lobbyState, raceParticipant, raceResult } from "../../test/fixtures";
import { renderPage } from "../../test/renderPage";
import { enterLobby } from "../../test/store";
import Lobby from "./Lobby";

async function renderLobby(userId = "p1") {
  const hub = fakeGameHub();
  const { store } = renderPage(<Lobby />, {
    route: "/lobby/l1",
    probes: ["/lobbies"],
    hub,
  });
  await enterLobby(store, hub, { userId, userName: "Player", role: "User" });
  return hub;
}

function typingInput() {
  return screen.getByRole("textbox") as HTMLInputElement;
}

describe("Lobby", () => {
  it("returns to the lobby list when the current user is kicked", async () => {
    const hub = await renderLobby();

    await act(() => hub.client().kicked());

    expect(await screen.findByText("/lobbies")).toBeTruthy();
  });

  it("returns to the lobby list after leaving", async () => {
    await renderLobby();

    await userEvent
      .setup()
      .click(await screen.findByRole("button", { name: "leave lobby" }));

    expect(await screen.findByText("/lobbies")).toBeTruthy();
  });

  it("stays in the lobby and shows the hub error when leaving fails", async () => {
    const hub = await renderLobby();
    hub.server.leaveLobby.mockRejectedValueOnce(
      new Error("HubException: Race in progress"),
    );

    await userEvent
      .setup()
      .click(await screen.findByRole("button", { name: "leave lobby" }));

    expect(await screen.findByText("Race in progress")).toBeTruthy();
    expect(screen.queryByText("/lobbies")).toBeNull();
    expect(screen.getByRole("heading", { name: "Lobby" })).toBeTruthy();
  });

  it("shows the start button to the host", async () => {
    await renderLobby("p1");

    expect(
      await screen.findByRole("button", { name: "start race" }),
    ).toBeTruthy();
  });

  it("hides the start button from a non-host", async () => {
    await renderLobby("p2");

    await screen.findByRole("heading", { name: "Lobby" });
    expect(screen.queryByRole("button", { name: "start race" })).toBeNull();
  });

  it("locks typing during the countdown", async () => {
    const hub = await renderLobby();

    await act(async () => {
      await hub.client().raceStarting({ countdownSeconds: 3 });
      await hub.client().lobbyState(lobbyState({ isRaceStarting: true }));
    });

    expect(typingInput().disabled).toBe(true);
    expect(screen.getByText("3")).toBeTruthy();
  });

  it("unlocks typing when the race starts", async () => {
    const hub = await renderLobby();
    await act(() => hub.client().raceStarting({ countdownSeconds: 3 }));

    await act(() => hub.client().raceStarted());

    expect(typingInput().disabled).toBe(false);
    expect(screen.queryByText("3")).toBeNull();
  });

  it("resumes the race after a reload when the lobby reports it started", async () => {
    const hub = await renderLobby();

    await act(() =>
      hub.client().lobbyState(lobbyState({ isRaceStarted: true })),
    );

    expect(typingInput().disabled).toBe(false);
  });

  it("sends typed progress to the server", async () => {
    const hub = await renderLobby();
    await act(() => hub.client().raceStarted());
    await act(() =>
      hub
        .client()
        .raceState([
          raceParticipant({ index: -1, typed: "", wpm: 0, mistakes: 0 }),
        ]),
    );

    await userEvent.setup().type(typingInput(), "h");

    expect(hub.server.updateRaceState).toHaveBeenCalledWith(0, 0, "h");
  });

  it("shows the results after the race ends and the typing area after closing them", async () => {
    const hub = await renderLobby();
    await act(() => hub.client().raceStarted());

    await act(() => hub.client().raceEnded({ results: [raceResult()] }));
    expect(screen.getByText("race results")).toBeTruthy();

    await userEvent
      .setup()
      .click(screen.getByRole("button", { name: "close" }));

    expect(screen.queryByText("race results")).toBeNull();
    expect(typingInput().disabled).toBe(true);
  });

  it("locks typing after being withdrawn from the race", async () => {
    const hub = await renderLobby();
    await act(() => hub.client().raceStarted());

    await act(() => hub.client().raceWithdrawn());

    expect(typingInput().disabled).toBe(true);
  });

  it("hides the next-passage button during a race", async () => {
    const hub = await renderLobby();
    expect(await screen.findByRole("button", { name: ">" })).toBeTruthy();

    await act(() => hub.client().raceStarted());

    expect(screen.queryByRole("button", { name: ">" })).toBeNull();
  });

  it("disables the start button during the countdown and the race", async () => {
    const hub = await renderLobby();
    const start = await screen.findByRole("button", { name: "start race" });
    expect(start.hasAttribute("disabled")).toBe(false);

    await act(() => hub.client().raceStarting({ countdownSeconds: 3 }));
    expect(start.hasAttribute("disabled")).toBe(true);

    await act(() => hub.client().raceStarted());
    expect(start.hasAttribute("disabled")).toBe(true);

    await act(() => hub.client().raceEnded({ results: [raceResult()] }));
    expect(start.hasAttribute("disabled")).toBe(false);
  });
});
