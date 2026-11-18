import { describe, expect, it } from "vitest";
import { FALLBACK_MESSAGE } from "../utils/apiErrorMessage";
import { CONNECTION_FAILED_MESSAGE, errorMessage } from "./errorMessage";

describe("errorMessage", () => {
  it("returns the reason from the response body for an HTTP status", () => {
    const message = errorMessage({
      status: 401,
      data: {
        reason: "Invalid login or password",
        code: "invalid_credentials",
      },
    });

    expect(message).toBe("Invalid login or password");
  });

  it("returns the generic message for a body that is not JSON", () => {
    const message = errorMessage({
      status: "PARSING_ERROR",
      originalStatus: 502,
      data: "<html>",
      error: "SyntaxError: Unexpected token <",
    });

    expect(message).toBe(FALLBACK_MESSAGE);
  });

  it("returns the connection message when the request cannot be sent", () => {
    const message = errorMessage({
      status: "FETCH_ERROR",
      error: "TypeError: Failed to fetch",
    });

    expect(message).toBe(CONNECTION_FAILED_MESSAGE);
  });

  it("returns the generic message for a serialized error", () => {
    const message = errorMessage({ message: "boom" });

    expect(message).toBe(FALLBACK_MESSAGE);
  });
});
