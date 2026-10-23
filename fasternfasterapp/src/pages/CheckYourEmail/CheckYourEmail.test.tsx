import { act, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { API_BASE } from "../../config";
import { renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import CheckYourEmail from "./CheckYourEmail";

async function renderAfterCooldown() {
  renderPage(<CheckYourEmail />, {
    route: "/check-your-email?email=a%40b.co",
  });
  for (let second = 0; second < 15; second++) {
    await act(() => vi.advanceTimersByTimeAsync(1000));
  }
  const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
  await user.click(screen.getByRole("button", { name: "resend link" }));
}

describe("CheckYourEmail", () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("starts the countdown after the server accepts the resend", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/resend-verification`, () =>
        HttpResponse.json({}),
      ),
    );

    await renderAfterCooldown();

    expect(await screen.findByText(/resend in \d+s/)).toBeTruthy();
  });

  it("shows the server reason and keeps the countdown idle when resend fails", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/resend-verification`, () =>
        HttpResponse.json(
          {
            reason: "Too many requests, try again shortly",
            code: "rate_limited",
          },
          { status: 429 },
        ),
      ),
    );

    await renderAfterCooldown();

    expect(
      await screen.findByText("Too many requests, try again shortly"),
    ).toBeTruthy();
    expect(screen.getByRole("button", { name: "resend link" })).toBeTruthy();
  });
});
