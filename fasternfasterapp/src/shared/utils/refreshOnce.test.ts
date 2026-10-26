import { API_BASE } from "../../config";
import { refreshOnce } from "./refreshOnce";
import { describe, it, expect, vi, afterEach, beforeEach } from "vitest";

describe("refreshOnce", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("sends a POST request with credentials", async () => {
    vi.mocked(fetch).mockResolvedValue(new Response());

    await refreshOnce();

    expect(fetch).toHaveBeenCalledWith(`${API_BASE}/api/auth/refresh`, {
      method: "POST",
      credentials: "include",
    });
  });

  it("fetches once for two concurrent requests", async () => {
    vi.mocked(fetch).mockResolvedValue(new Response());

    const [result1, result2] = await Promise.all([
      refreshOnce(),
      refreshOnce(),
    ]);

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(result1).toBe(result2);
  });

  it("allows a new request after the previous one completes", async () => {
    vi.mocked(fetch).mockResolvedValue(new Response());

    await refreshOnce();
    await refreshOnce();

    expect(fetch).toHaveBeenCalledTimes(2);
  });

  it("allows retry after a failed request", async () => {
    vi.mocked(fetch)
      .mockRejectedValueOnce(new Error("Network error"))
      .mockResolvedValueOnce(new Response());

    await expect(refreshOnce()).rejects.toThrow("Network error");

    await refreshOnce();

    expect(fetch).toHaveBeenCalledTimes(2);
  });
});
