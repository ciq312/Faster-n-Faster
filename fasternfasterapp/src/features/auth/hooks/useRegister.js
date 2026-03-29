import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { extractError } from "../../../shared/utils/extractError";

export function useRegister() {
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const execute = async ({ nick, login, password }) => {
    setError(null);
    setLoading(true);
    try {
      const response = await fetch("/api/auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nick, login, password }),
      });
      if (!response.ok) {
        setError(await extractError(response));
        return;
      }
      const data = await response.json();
      localStorage.setItem("token", data.token);
      localStorage.setItem("userId", data.userId);
      navigate("/lobbies");
    } catch {
      setError("Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  return { execute, error, loading, setError };
}
