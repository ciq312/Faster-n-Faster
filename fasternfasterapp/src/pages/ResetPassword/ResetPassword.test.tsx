import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { API_BASE } from "../../config";
import { renderPage } from "../../test/renderPage";
import { server } from "../../test/server";
import ResetPassword from "./ResetPassword";

async function resetPassword() {
  renderPage(<ResetPassword />, {
    route: "/reset-password?token=abc",
    probes: ["/"],
  });
  const user = userEvent.setup();
  await user.type(screen.getByPlaceholderText("new password"), "secret");
  await user.type(screen.getByPlaceholderText("repeat password"), "secret");
  await user.click(screen.getByRole("button", { name: "update password" }));
}

describe("ResetPassword", () => {
  it("returns home with a confirmation after the password is updated", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/reset-password`, () =>
        HttpResponse.json({}),
      ),
    );

    await resetPassword();

    expect(await screen.findByText("/")).toBeTruthy();
    expect(screen.getByText("password updated — please log in")).toBeTruthy();
  });

  it("shows the server reason when the reset fails", async () => {
    server.use(
      http.post(`${API_BASE}/api/auth/reset-password`, () =>
        HttpResponse.json(
          { reason: "Reset link expired", code: "invalid_token" },
          { status: 400 },
        ),
      ),
    );

    await resetPassword();

    expect(await screen.findByText("Reset link expired")).toBeTruthy();
  });
});
