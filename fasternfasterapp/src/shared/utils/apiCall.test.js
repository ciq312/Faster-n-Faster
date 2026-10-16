import { apiCall } from "./apiCall";
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { refreshOnce } from "./refreshOnce.js";

vi.mock("./refreshOnce.js", () => ({
  refreshOnce: vi.fn(),
}));

describe("apiCall", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("should make a successful API call once", async () => {
    fetch.mockResolvedValue({
      ok: true,
    });
    const response = await apiCall("/test-endpoint");
    expect(response.ok).toBe(true);
    expect(fetch).toHaveBeenCalledTimes(1);
  });

  it("should fetch two times if first is rejected", async () => {
    fetch.mockResolvedValueOnce({ status: 401 });
    fetch.mockResolvedValueOnce({
      ok: true,
    });

    vi.mocked(refreshOnce).mockResolvedValue({ ok: true });

    const response = await apiCall("/test-endpoint");
    expect(response.ok).toBe(true);
    expect(fetch).toHaveBeenCalledTimes(2);
    expect(refreshOnce).toHaveBeenCalledTimes(1);
  });

  it("should return 401 if token refresh fails", async () => {
    fetch.mockResolvedValueOnce({ status: 401 });

    vi.mocked(refreshOnce).mockResolvedValue({ ok: false });

    const response = await apiCall("/test-endpoint");
    expect(response.status).toBe(401);
    expect(fetch).toHaveBeenCalledTimes(1);
    expect(refreshOnce).toHaveBeenCalledTimes(1);
  });
});
