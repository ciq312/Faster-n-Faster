import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { API_BASE } from "../../config";
import { renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import ForgotPassword from "./ForgotPassword";

async function submitEmail() {
  renderPage(<ForgotPassword />);
  const user = userEvent.setup();
  await user.type(
    screen.getByPlaceholderText("you@example.com"),
    "player@example.com",
  );
  await user.click(screen.getByRole("button", { name: "send reset link" }));
}

describe("ForgotPassword", () => {
  it("shows the confirmation after the server accepts the request", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/forgot-password`, () =>
        HttpResponse.json({}),
      ),
    );

    await submitEmail();

    expect(await screen.findByText(/a reset link is on its way/)).toBeTruthy();
  });

  it("shows the server reason and keeps the form when the request fails", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/forgot-password`, () =>
        HttpResponse.json(
          {
            reason: "Too many requests, try again shortly",
            code: "rate_limited",
          },
          { status: 429 },
        ),
      ),
    );

    await submitEmail();

    expect(
      await screen.findByText("Too many requests, try again shortly"),
    ).toBeTruthy();
    expect(
      await screen.findByRole("button", { name: "send reset link" }),
    ).toBeTruthy();
  });
});
