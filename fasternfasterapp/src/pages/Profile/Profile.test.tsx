import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { API_BASE } from "../../config";
import { deferred } from "../../test/deferred";
import { fakeGameHub } from "../../test/fakeGameHub";
import { CurrentUser, renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import Profile from "./Profile";

const CHANGE_NICK_URL = `${API_BASE}/api/users/me/nick`;

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

function signedInAs(role: string) {
  server.use(
    http.get(`${API_BASE}/api/auth/me`, () =>
      HttpResponse.json({ userId: "p1", userName: "Player", role }),
    ),
    http.get(`${API_BASE}/api/users/me/profile`, () =>
      HttpResponse.json({ nick: "Player" }),
    ),
  );
}

async function editNick(keys: string) {
  const user = userEvent.setup();
  await user.click(await screen.findByRole("button", { name: "edit nick" }));
  const input = screen.getByRole("textbox", { name: "nick" });
  await user.clear(input);
  if (keys) await user.type(input, keys);
  return user;
}

describe("Profile", () => {
  it("saves the new nick from the check button, confirms it and restarts the hub", async () => {
    let sentBody: unknown;
    signedInAs("Player");
    server.use(
      http.put(CHANGE_NICK_URL, async ({ request }) => {
        sentBody = await request.json();
        return HttpResponse.json({});
      }),
    );
    const hub = renderProfile();
    const nick = "Speedy";
    const user = await editNick(nick);
    await user.click(screen.getByRole("button", { name: "save nick" }));

    expect(await screen.findByText(`You changed nick to ${nick}`)).toBeTruthy();
    await waitFor(() => expect(hub.start).toHaveBeenCalledTimes(1));
    expect(hub.stop).toHaveBeenCalledTimes(1);
    expect(sentBody).toEqual({ nick: "Speedy" });
  });

  it("saves the new nick when Enter is pressed", async () => {
    signedInAs("Player");
    server.use(http.put(CHANGE_NICK_URL, () => HttpResponse.json({})));
    renderProfile();
    const nick = "Speedy";
    await editNick(`${nick}{Enter}`);

    expect(await screen.findByText(`You changed nick to ${nick}`)).toBeTruthy();
  });

  it("shows loading while saving, then the check mark, then the edit button again", async () => {
    const gate = deferred();
    signedInAs("Player");
    server.use(
      http.put(CHANGE_NICK_URL, async () => {
        await gate.promise;
        return HttpResponse.json({});
      }),
    );
    renderProfile();

    await editNick("Speedy{Enter}");

    expect(
      await screen.findByRole("button", { name: "saving nick" }),
    ).toBeTruthy();
    gate.resolve();
    expect(
      await screen.findByRole("button", { name: "nick saved" }),
    ).toBeTruthy();
    expect(
      await screen.findByRole(
        "button",
        { name: "edit nick" },
        { timeout: 2000 },
      ),
    ).toBeTruthy();
  });

  it("shows the server reason and keeps editing when saving fails", async () => {
    signedInAs("Player");
    server.use(
      http.put(CHANGE_NICK_URL, () =>
        HttpResponse.json(
          {
            reason: "Too many requests, try again shortly",
            code: "rate_limited",
          },
          { status: 429 },
        ),
      ),
    );
    const hub = renderProfile();

    await editNick("Speedy{Enter}");

    expect(
      await screen.findByText("Too many requests, try again shortly"),
    ).toBeTruthy();
    expect(screen.getByRole("textbox", { name: "nick" })).toBeTruthy();
    expect(hub.stop).not.toHaveBeenCalled();
  });

  it("stops editing without a request when the nick is unchanged", async () => {
    let requested = false;
    signedInAs("Player");
    server.use(
      http.put(CHANGE_NICK_URL, () => {
        requested = true;
        return HttpResponse.json({});
      }),
    );
    renderProfile();

    await editNick("Player{Enter}");

    expect(
      await screen.findByRole("button", { name: "edit nick" }),
    ).toBeTruthy();
    expect(requested).toBe(false);
    expect(screen.queryByText(/You changed nick/)).toBeNull();
  });

  it("restores the nick when Escape is pressed", async () => {
    signedInAs("Player");
    renderProfile();

    await editNick("Speedy{Escape}");

    expect(await screen.findByRole("heading", { name: "Player" })).toBeTruthy();
    expect(screen.queryByRole("textbox", { name: "nick" })).toBeNull();
  });

  it("does not allow saving an empty nick", async () => {
    signedInAs("Player");
    renderProfile();

    await editNick("");

    expect(
      screen.getByRole<HTMLButtonElement>("button", { name: "save nick" })
        .disabled,
    ).toBe(true);
  });

  it("offers no nick editing to a guest", async () => {
    signedInAs("Guest");

    renderProfile();

    expect(
      await screen.findByRole("button", { name: "register now" }),
    ).toBeTruthy();
    expect(screen.queryByRole("button", { name: "edit nick" })).toBeNull();
  });

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
