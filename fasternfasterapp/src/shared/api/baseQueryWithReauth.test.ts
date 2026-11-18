import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { setupStore } from "../../app/store";
import { API_BASE } from "../../config";
import { deferred } from "../../test/deferred";
import { server } from "../../test/server";
import { api } from "./api";

const probeApi = api.injectEndpoints({
  endpoints: (builder) => ({
    probe: builder.query<unknown, number>({
      query: (n) => `/api/probe/${n}`,
    }),
    probeWithoutReauth: builder.query<unknown, number>({
      query: (n) => `/api/probe/${n}`,
      extraOptions: { reauth: false },
    }),
  }),
});

function serveProbe({ refresh }: { refresh: () => Promise<Response> }) {
  const hits = { probe: 0, refresh: 0 };
  let refreshed = false;
  server.use(
    http.get(`${API_BASE}/api/probe/:n`, () => {
      hits.probe++;
      return refreshed
        ? HttpResponse.json({ ok: true })
        : HttpResponse.json({}, { status: 401 });
    }),
    http.post(`${API_BASE}/api/auth/refresh`, async () => {
      hits.refresh++;
      const response = await refresh();
      refreshed = response.ok;
      return response;
    }),
  );
  return hits;
}

const accepted = () => Promise.resolve(HttpResponse.json({}));
const rejected = () => Promise.resolve(HttpResponse.json({}, { status: 401 }));

describe("baseQueryWithReauth", () => {
  it("retries the request once after a successful refresh", async () => {
    const hits = serveProbe({ refresh: accepted });
    const store = setupStore();

    const result = await store.dispatch(probeApi.endpoints.probe.initiate(1));

    expect(result.data).toEqual({ ok: true });
    expect(hits).toEqual({ probe: 2, refresh: 1 });
  });

  it("returns the original 401 when the refresh is rejected", async () => {
    const hits = serveProbe({ refresh: rejected });
    const store = setupStore();

    const result = await store.dispatch(probeApi.endpoints.probe.initiate(1));

    expect(result.error).toMatchObject({ status: 401 });
    expect(hits).toEqual({ probe: 1, refresh: 1 });
  });

  it("returns the original 401 when the refresh request cannot be sent", async () => {
    const hits = serveProbe({
      refresh: () => Promise.resolve(HttpResponse.error()),
    });
    const store = setupStore();

    const result = await store.dispatch(probeApi.endpoints.probe.initiate(1));

    expect(result.error).toMatchObject({ status: 401 });
    expect(hits).toEqual({ probe: 1, refresh: 1 });
  });

  it("shares one refresh between two concurrent 401s", async () => {
    const bothRejected = deferred();
    const hits = serveProbe({
      refresh: async () => {
        await bothRejected.promise;
        return HttpResponse.json({});
      },
    });
    let rejections = 0;
    server.events.on("response:mocked", ({ request }) => {
      if (request.url.includes("/api/probe/") && ++rejections === 2) {
        bothRejected.resolve();
      }
    });
    const store = setupStore();

    const results = await Promise.all([
      store.dispatch(probeApi.endpoints.probe.initiate(1)),
      store.dispatch(probeApi.endpoints.probe.initiate(2)),
    ]);

    expect(results.map((r) => r.data)).toEqual([{ ok: true }, { ok: true }]);
    expect(hits.refresh).toBe(1);
  });

  it("does not refresh for an endpoint that opts out of reauth", async () => {
    const hits = serveProbe({ refresh: accepted });
    const store = setupStore();

    const result = await store.dispatch(
      probeApi.endpoints.probeWithoutReauth.initiate(1),
    );

    expect(result.error).toMatchObject({ status: 401 });
    expect(hits).toEqual({ probe: 1, refresh: 0 });
  });

  it("passes other errors through without refreshing", async () => {
    const hits = { refresh: 0 };
    server.use(
      http.get(`${API_BASE}/api/probe/:n`, () =>
        HttpResponse.json({}, { status: 500 }),
      ),
      http.post(`${API_BASE}/api/auth/refresh`, () => {
        hits.refresh++;
        return HttpResponse.json({});
      }),
    );
    const store = setupStore();

    const result = await store.dispatch(probeApi.endpoints.probe.initiate(1));

    expect(result.error).toMatchObject({ status: 500 });
    expect(hits.refresh).toBe(0);
  });
});
