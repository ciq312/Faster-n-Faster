import { extractHttpError } from "./extractHttpError";
import { describe, it, expect } from "vitest";

const FALLBACK_MESSAGE = "Something went wrong, try again";

function response(status, json) {
  return { status, json: async () => json };
}

describe("extractHttpError", () => {
  it("returns the first message of the first non-empty errors entry for a validation failure", async () => {
    const res = response(400, {
      reason: "One or more validation errors occurred.",
      code: "validation_failed",
      errors: { Name: [], Email: ["Email is invalid", "Email is taken"] },
    });

    expect(await extractHttpError(res)).toBe("Email is invalid");
  });

  it("uses reason when there are no errors", async () => {
    const res = response(409, { reason: "Lobby is full", code: "lobby_full" });

    expect(await extractHttpError(res)).toBe("Lobby is full");
  });

  it("uses reason and ignores errors that carry data for other codes", async () => {
    const res = response(403, {
      reason: "Email verification required",
      code: "email_not_verified",
      errors: { email: ["player@example.com"] },
    });

    expect(await extractHttpError(res)).toBe("Email verification required");
  });

  it("falls back to the generic message for an empty reason", async () => {
    const res = response(400, { reason: "", code: "bad_request" });

    expect(await extractHttpError(res)).toBe(FALLBACK_MESSAGE);
  });

  it("falls back to the generic message for an empty object", async () => {
    expect(await extractHttpError(response(400, {}))).toBe(FALLBACK_MESSAGE);
  });

  it("falls back to the generic message for a non-object body", async () => {
    expect(await extractHttpError(response(500, null))).toBe(FALLBACK_MESSAGE);
    expect(await extractHttpError(response(502, "text"))).toBe(
      FALLBACK_MESSAGE,
    );
  });

  it("returns the generic message when the body is not JSON", async () => {
    const res = {
      status: 500,
      json: async () => {
        throw new SyntaxError();
      },
    };

    expect(await extractHttpError(res)).toBe(FALLBACK_MESSAGE);
  });
});
