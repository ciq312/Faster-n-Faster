import { http, HttpResponse } from "msw";
import { API_BASE } from "../config";

export const signedOut = () => [
  http.get(`${API_BASE}/api/auth/me`, () =>
    HttpResponse.json({}, { status: 401 }),
  ),
  http.post(`${API_BASE}/api/auth/refresh`, () =>
    HttpResponse.json({}, { status: 401 }),
  ),
];
