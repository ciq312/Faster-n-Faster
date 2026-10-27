import { beforeAll, afterEach, afterAll, vi } from "vitest";
import { server } from "./server";
import { cleanup } from "@testing-library/react";
import {
  FakeIntersectionObserver,
  resetObservers,
} from "./fakeIntersectionObserver";

vi.stubGlobal("IntersectionObserver", FakeIntersectionObserver);

beforeAll(() => server.listen({ onUnhandledFrame: "error" }));
afterEach(() => {
  server.resetHandlers();
  server.events.removeAllListeners();
  cleanup();
  resetObservers();
});
afterAll(() => server.close());
