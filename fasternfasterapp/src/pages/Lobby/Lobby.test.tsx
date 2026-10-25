import { act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { fakeGameHub } from "../../test/fakeGameHub";
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
});
