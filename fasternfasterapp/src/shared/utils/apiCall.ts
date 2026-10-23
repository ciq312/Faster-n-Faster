import { refreshOnce } from "./refreshOnce";
import { API_BASE } from "../../config";

export async function apiCall(url: string, options: RequestInit = {}) {
  const request = () =>
    fetch(`${API_BASE}${url}`, {
      credentials: "include",
      ...options,
    });

  const response = await request();

  if (response.status === 401) {
    const refreshResponse = await refreshOnce();
    if (!refreshResponse.ok) return response;
    const secondResponse = await request();
    return secondResponse;
  }
  return response;
}
