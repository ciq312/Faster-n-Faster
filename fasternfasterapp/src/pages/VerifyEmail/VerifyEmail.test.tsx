import { act, screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { API_BASE } from "../../config";
import { renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import VerifyEmail from "./VerifyEmail";

async function renderVerifyEmail(search: string) {
  renderPage(<VerifyEmail />, { route: `/verify-email${search}` });
  await act(() => vi.advanceTimersByTimeAsync(1000));
}

describe("VerifyEmail", () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it("reports success after the server verifies the token", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/verify-email`, () =>
        HttpResponse.json({}),
      ),
    );

    await renderVerifyEmail("?token=abc");

    expect(
      await screen.findByText("your email is verified. you can now log in."),
    ).toBeTruthy();
  });

  it("reports failure when verification is rejected", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/verify-email`, () =>
        HttpResponse.json(
          { reason: "Token expired", code: "invalid_token" },
          { status: 400 },
        ),
      ),
    );

    await renderVerifyEmail("?token=abc");

    expect(await screen.findByText(/invalid or expired link/)).toBeTruthy();
  });

  it("reports a missing token without calling the server", async () => {
    let hits = 0;
    server.use(
      http.post(`${API_BASE}/api/auth/verify-email`, () => {
        hits++;
        return HttpResponse.json({});
      }),
    );

    await renderVerifyEmail("");

    expect(screen.getByText("no token provided.")).toBeTruthy();
    expect(hits).toBe(0);
  });
});
