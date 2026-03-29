import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { extractError } from "../../../shared/utils/extractError";

export function useLogin() {
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const execute = async ({ login, password }) => {
    setError(null);
    setLoading(true);
    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ login, password }),
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
