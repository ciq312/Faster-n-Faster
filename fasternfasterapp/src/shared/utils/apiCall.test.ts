import { apiCall } from "./apiCall";
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { refreshOnce } from "./refreshOnce";

vi.mock("./refreshOnce", () => ({
  refreshOnce: vi.fn(),
}));

const respond = (status: number) => new Response(null, { status });

describe("apiCall", () => {
  const fetchMock = vi.fn<typeof fetch>();

  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal("fetch", fetchMock);
  });
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("calls the API once when it succeeds", async () => {
    fetchMock.mockResolvedValue(respond(200));
    const response = await apiCall("/test-endpoint");
    expect(response.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("retries once after a 401 and a successful refresh", async () => {
    fetchMock.mockResolvedValueOnce(respond(401));
    fetchMock.mockResolvedValueOnce(respond(200));

    vi.mocked(refreshOnce).mockResolvedValue(respond(200));

    const response = await apiCall("/test-endpoint");
    expect(response.ok).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(2);
    expect(refreshOnce).toHaveBeenCalledTimes(1);
  });

  it("returns the 401 when the refresh fails", async () => {
    fetchMock.mockResolvedValueOnce(respond(401));

    vi.mocked(refreshOnce).mockResolvedValue(respond(401));

    const response = await apiCall("/test-endpoint");
    expect(response.status).toBe(401);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(refreshOnce).toHaveBeenCalledTimes(1);
  });
});
