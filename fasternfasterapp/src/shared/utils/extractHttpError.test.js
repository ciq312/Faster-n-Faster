import { extractHttpError } from "./extractHttpError";
import { describe, it, expect } from "vitest";

const FALLBACK_MESSAGE = "Something went wrong, try again";

function response(status, json) {
  return { status, json: async () => json };
}

describe("extractHttpError", () => {
  it("returns the first message of the first non-empty errors entry", async () => {
    const res = response(400, {
      errors: { Name: [], Email: ["Email is invalid", "Email is taken"] },
    });

    expect(await extractHttpError(res)).toBe("Email is invalid");
  });

  it("prefers errors over error and message", async () => {
    const res = response(400, {
      errors: { Name: ["Name is required"] },
      error: "error text",
      message: "message text",
    });

    expect(await extractHttpError(res)).toBe("Name is required");
  });

  it("uses error when there are no errors", async () => {
    const res = response(400, { error: "error text", message: "message text" });

    expect(await extractHttpError(res)).toBe("error text");
  });

  it("skips an empty error string and uses message", async () => {
    const res = response(400, { error: "", message: "message text" });

    expect(await extractHttpError(res)).toBe("message text");
  });

  it("falls back to the status for an empty object", async () => {
    expect(await extractHttpError(response(400, {}))).toBe("Error 400");
  });

  it("falls back to the status for a non-object body", async () => {
    expect(await extractHttpError(response(500, null))).toBe("Error 500");
    expect(await extractHttpError(response(502, "text"))).toBe("Error 502");
  });

  it("returns a generic message when the body is not JSON", async () => {
    const res = {
      status: 500,
      json: async () => {
        throw new SyntaxError();
      },
    };

    expect(await extractHttpError(res)).toBe(FALLBACK_MESSAGE);
  });
});
