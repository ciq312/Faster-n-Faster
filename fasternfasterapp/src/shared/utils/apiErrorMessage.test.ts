import { describe, expect, it } from "vitest";
import { apiErrorMessage, FALLBACK_MESSAGE } from "./apiErrorMessage";

describe("apiErrorMessage", () => {
  it("returns the first validation message for a validation failure", () => {
    const message = apiErrorMessage({
      reason: "Validation failed",
      code: "validation_failed",
      errors: { password: ["Password is too short", "Password is weak"] },
    });

    expect(message).toBe("Password is too short");
  });

  it("returns the reason for other error codes", () => {
    const message = apiErrorMessage({
      reason: "Invalid login or password",
      code: "invalid_credentials",
    });

    expect(message).toBe("Invalid login or password");
  });

  it("ignores errors that carry data for other codes", () => {
    const message = apiErrorMessage({
      reason: "Email is not verified",
      code: "email_not_verified",
      errors: { email: ["player@example.com"] },
    });

    expect(message).toBe("Email is not verified");
  });

  it("returns the generic message for a body that is not an error response", () => {
    expect(apiErrorMessage("<html>")).toBe(FALLBACK_MESSAGE);
    expect(apiErrorMessage(null)).toBe(FALLBACK_MESSAGE);
    expect(apiErrorMessage({ reason: "no code" })).toBe(FALLBACK_MESSAGE);
  });
});
