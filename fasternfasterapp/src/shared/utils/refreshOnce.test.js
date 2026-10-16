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

  it("should send POST request with creadentials", async () => {
    fetch.mockResolvedValue({ ok: true });

    await refreshOnce();

    expect(fetch).toHaveBeenCalledWith(`${API_BASE}/api/auth/refresh`, {
      method: "POST",
      credentials: "include",
    });
  });

  it("should fetch once if two concurrent requests are made", async () => {
    fetch.mockResolvedValue({ ok: true });

    const [result1, result2] = await Promise.all([
      refreshOnce(),
      refreshOnce(),
    ]);

    expect(fetch).toHaveBeenCalledTimes(1);
    expect(result1).toBe(result2);
  });

  it("allows a new request after the previous one completes", async () => {
    fetch.mockResolvedValue({ ok: true });

    await refreshOnce();
    await refreshOnce();

    expect(fetch).toHaveBeenCalledTimes(2);
  });

  it("allows retry after a failed request", async () => {
    fetch
      .mockRejectedValueOnce(new Error("Network error"))
      .mockResolvedValueOnce({ ok: true });

    await expect(refreshOnce()).rejects.toThrow("Network error");

    await refreshOnce();

    expect(fetch).toHaveBeenCalledTimes(2);
  });
});
