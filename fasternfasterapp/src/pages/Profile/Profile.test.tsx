import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { API_BASE } from "../../config";
import { LobbyProvider } from "../../features/game/hooks/LobbyProvider";
import { CurrentUser, renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import Profile from "./Profile";

const connection = vi.hoisted(() => ({ disconnect: vi.fn() }));

vi.mock("../../features/connection/ConnectionProvider", () => ({
  useConnection: () => connection,
}));

async function logOut() {
  server.use(
    http.get(`${API_BASE}/api/auth/me`, () =>
      HttpResponse.json({ userId: "p1", userName: "Player", role: "User" }),
    ),
    http.get(`${API_BASE}/api/users/me/profile`, () =>
      HttpResponse.json({ dto: { nick: "Player" } }),
    ),
  );
  renderPage(
    <LobbyProvider>
      <CurrentUser />
      <Profile />
    </LobbyProvider>,
    { route: "/profile/me", probes: ["/"] },
  );
  await userEvent
    .setup()
    .click(await screen.findByRole("button", { name: "log out" }));
}

describe("Profile", () => {
  beforeEach(() => {
    connection.disconnect.mockClear();
  });

  it("disconnects the hub and returns home after logout", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/logout`, () => HttpResponse.json({})),
    );

    await logOut();

    expect(await screen.findByText("/")).toBeTruthy();
    expect(connection.disconnect).toHaveBeenCalledTimes(1);
  });

  it("shows the server reason and stays on the profile when logout fails", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/logout`, () =>
        HttpResponse.json(
          { reason: "Could not log out", code: "internal_error" },
          { status: 500 },
        ),
      ),
    );

    await logOut();

    expect(await screen.findByText("Could not log out")).toBeTruthy();
    expect(await screen.findByRole("button", { name: "log out" })).toBeTruthy();
    expect(connection.disconnect).not.toHaveBeenCalled();
  });
});
