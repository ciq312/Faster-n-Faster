import { beforeAll, afterEach, afterAll } from "vitest";
import { server } from "./server";
import { cleanup } from "@testing-library/react";

beforeAll(() => server.listen({ onUnhandledFrame: "error" }));
afterEach(() => {
  server.resetHandlers();
  server.events.removeAllListeners();
  cleanup();
});
afterAll(() => server.close());
