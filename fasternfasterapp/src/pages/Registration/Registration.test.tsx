import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { API_BASE } from "../../config";
import { signedOut } from "../../test/handlers";
import { CurrentUser, renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import Registration from "./Registration";

function renderRegistration() {
  server.use(...signedOut());
  renderPage(
    <>
      <CurrentUser />
      <Registration />
    </>,
    { probes: ["/check-your-email", "/lobbies"] },
  );
}

async function logIn() {
  const user = userEvent.setup();
  await user.click(screen.getByRole("button", { name: "Log in" }));
  await user.type(screen.getByPlaceholderText("username or email"), "player");
  await user.type(screen.getByPlaceholderText("enter password"), "secret");
  await user.click(screen.getByRole("button", { name: "log in" }));
}

async function joinAsGuest() {
  const user = userEvent.setup();
  await user.type(screen.getByPlaceholderText("enter your name"), "Player");
  await user.click(screen.getByRole("button", { name: "play" }));
}

async function signUp() {
  const user = userEvent.setup();
  await user.click(screen.getByRole("button", { name: "Sign up" }));
  await user.type(screen.getByPlaceholderText("your username"), "player");
  await user.type(
    screen.getByPlaceholderText("you@example.com"),
    "player@example.com",
  );
  await user.type(screen.getByPlaceholderText("display name"), "Player");
  await user.type(screen.getByPlaceholderText("create password"), "secret");
  await user.type(screen.getByPlaceholderText("repeat password"), "secret");
  await user.click(screen.getByRole("button", { name: "sign up" }));
}

describe("Registration", () => {
  it("redirects to check-your-email when login returns email_not_verified", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/login`, () =>
        HttpResponse.json(
          {
            reason: "Email is not verified",
            code: "email_not_verified",
            errors: { email: ["player@example.com"] },
          },
          { status: 403 },
        ),
      ),
    );
    renderRegistration();

    await logIn();

    expect(
      await screen.findByText("/check-your-email?email=player%40example.com"),
    ).toBeTruthy();
  });

  it("opens the lobbies after a successful login", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/login`, () => HttpResponse.json({})),
    );
    renderRegistration();

    await logIn();

    expect(await screen.findByText("/lobbies")).toBeTruthy();
  });

  it("shows the server reason in the banner when login fails", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/login`, () =>
        HttpResponse.json(
          {
            reason: "Invalid login or password",
            code: "invalid_credentials",
          },
          { status: 401 },
        ),
      ),
    );
    renderRegistration();

    await logIn();

    expect(await screen.findByText("Invalid login or password")).toBeTruthy();
  });

  it("opens the lobbies after joining as a guest", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/guest`, () => HttpResponse.json({})),
    );
    renderRegistration();

    await joinAsGuest();

    expect(await screen.findByText("/lobbies")).toBeTruthy();
  });

  it("shows the server reason when joining as a guest fails", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/guest`, () =>
        HttpResponse.json(
          { reason: "Nick is already taken", code: "nick_taken" },
          { status: 409 },
        ),
      ),
    );
    renderRegistration();

    await joinAsGuest();

    expect(await screen.findByText("Nick is already taken")).toBeTruthy();
  });

  it("opens check-your-email after sign-up", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/register`, () => HttpResponse.json({})),
    );
    renderRegistration();

    await signUp();

    expect(
      await screen.findByText("/check-your-email?email=player%40example.com"),
    ).toBeTruthy();
  });
});
