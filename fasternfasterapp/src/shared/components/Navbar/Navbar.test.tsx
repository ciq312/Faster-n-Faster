import { screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { selectLobbyId } from "../../../features/lobbies/lobbySelectors";
import { fakeGameHub } from "../../../test/fakeGameHub";
import { renderPage } from "../../../test/renderPage";
import { enterLobby } from "../../../test/store";
import Navbar from "./Navbar";

async function renderNavbarInLobby(route: string) {
  const hub = fakeGameHub();
  const { store } = renderPage(<Navbar />, { route, hub });
  await enterLobby(store, hub);
  return store;
}

describe("Navbar", () => {
  it("shows back to lobby once the current user's lobby state arrives", async () => {
    await renderNavbarInLobby("/leaderboard");

    expect(
      await screen.findByRole("button", { name: "back to lobby" }),
    ).toBeTruthy();
  });

  it("hides back to lobby on the lobby page", async () => {
    const store = await renderNavbarInLobby("/lobby/l1");

    expect(selectLobbyId(store.getState())).toBe("l1");
    expect(screen.queryByRole("button", { name: "back to lobby" })).toBeNull();
  });
});
