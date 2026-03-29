export async function extractError(response) {
  try {
    const body = await response.json();
    if (body.errors) {
      const firstKey = Object.keys(body.errors)[0];
      if (firstKey && body.errors[firstKey]?.length)
        return body.errors[firstKey][0];
    }
    return body.message || `Error ${response.status}`;
  } catch {
    return "Something went wrong, try again";
  }
}
