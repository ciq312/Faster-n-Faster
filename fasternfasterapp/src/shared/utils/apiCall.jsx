export async function apiCall(url, options) {
  let response = await fetch(url, { credentials: "include", ...options });
  if (response.status === 401) {
    const refreshResponse = await fetch("/api/auth/refresh", {
      method: "GET",
      credentials: "include",
    });
    if (!refreshResponse.ok) return response;
    response = await fetch(url, { credentials: "include", ...options });
  }
  return response;
}
