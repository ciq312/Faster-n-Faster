import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { API_BASE } from "../../config";
import { fakeGameHub } from "../../test/fakeGameHub";
import { CurrentUser, renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import Profile from "./Profile";

function renderProfile() {
  const hub = fakeGameHub();
  renderPage(
    <>
      <CurrentUser />
      <Profile />
    </>,
    { route: "/profile/me", probes: ["/"], hub },
  );
  return hub;
}

async function logOut() {
  server.use(
    http.get(`${API_BASE}/api/auth/me`, () =>
      HttpResponse.json({ userId: "p1", userName: "Player", role: "User" }),
    ),
    http.get(`${API_BASE}/api/users/me/profile`, () =>
      HttpResponse.json({ nick: "Player" }),
    ),
  );
  const hub = renderProfile();
  await userEvent
    .setup()
    .click(await screen.findByRole("button", { name: "log out" }));
  return hub;
}

describe("Profile", () => {
  it("disconnects the hub and returns home after logout", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/logout`, () => HttpResponse.json({})),
    );

    const hub = await logOut();

    expect(await screen.findByText("/")).toBeTruthy();
    expect(hub.stop).toHaveBeenCalledTimes(1);
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

    const hub = await logOut();

    expect(await screen.findByText("Could not log out")).toBeTruthy();
    expect(await screen.findByRole("button", { name: "log out" })).toBeTruthy();
    expect(hub.stop).not.toHaveBeenCalled();
  });

  it("shows the player's statistics", async () => {
    server.use(
      http.get(`${API_BASE}/api/auth/me`, () =>
        HttpResponse.json({ userId: "p1", userName: "Player", role: "User" }),
      ),
      http.get(`${API_BASE}/api/users/me/profile`, () =>
        HttpResponse.json({
          nick: "Speedy",
          wins: 3,
          racesTyped: 7,
          bestWPM: 88,
          avgWPM: 70,
          bestAccuracy: 99,
          avgAccuracy: 95,
          wordsTyped: 400,
        }),
      ),
    );

    renderProfile();

    expect(await screen.findByRole("heading", { name: "Speedy" })).toBeTruthy();
    expect(screen.getByText("88")).toBeTruthy();
  });
});
