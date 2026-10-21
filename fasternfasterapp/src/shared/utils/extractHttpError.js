export async function extractHttpError(response) {
  try {
    const body = await response.json();

    if (!body || typeof body !== "object") {
      return `Error ${response.status}`;
    }

    const errors = body.errors;

    if (errors && typeof errors === "object") {
      for (const messages of Object.values(errors)) {
        if (Array.isArray(messages) && messages.length > 0) {
          return messages[0];
        }
      }
    }

    if (typeof body.reason === "string" && body.reason) {
      return body.reason;
    }

    if (typeof body.message === "string" && body.message) {
      return body.message;
    }

    return `Error ${response.status}`;
  } catch {
    return "Something went wrong, try again";
  }
}
