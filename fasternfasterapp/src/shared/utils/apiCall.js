import { refreshOnce } from "./refreshOnce.js";
import { API_BASE } from "../../config.js";

export async function apiCall(url, options) {
  let response = await fetch(`${API_BASE}${url}`, {
    credentials: "include",
    ...options,
  });
  if (response.status === 401) {
    const refreshResponse = await refreshOnce();
    if (!refreshResponse.ok) return response;
    response = await fetch(`${API_BASE}${url}`, {
      credentials: "include",
      ...options,
    });
  }
  return response;
}
