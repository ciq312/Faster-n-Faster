import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { extractError } from "../../../shared/utils/extractError";
import { useError } from "../../../shared/components/BannerProvider";

export function useRegister() {
  const { showError } = useError();
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const execute = async ({ nick, login, password }) => {
    setLoading(true);
    try {
      const response = await fetch("/api/auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nick, login, password }),
      });
      if (!response.ok) {
        showError(await extractError(response));
        return;
      }
      const data = await response.json();
      localStorage.setItem("token", data.token);
      localStorage.setItem("userId", data.userId);
      navigate("/lobbies");
    } catch {
      showError("Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading };
}
