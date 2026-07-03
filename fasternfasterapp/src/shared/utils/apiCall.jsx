export const API_BASE = import.meta.env.VITE_API_URL ?? "";

export async function apiCall(url, options) {
  let response = await fetch(`${API_BASE}${url}`, { credentials: "include", ...options });
  if (response.status === 401) {
    const refreshResponse = await fetch(`${API_BASE}/api/auth/refresh`, {
      method: "POST",
      credentials: "include",
    });
    if (!refreshResponse.ok) return response;
    response = await fetch(`${API_BASE}${url}`, { credentials: "include", ...options });
  }
  return response;
}
