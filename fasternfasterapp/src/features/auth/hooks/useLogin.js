import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { extractError } from "../../../shared/utils/extractError";

export function useLogin() {
  const { showError } = useError();
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const execute = async ({ login, password }) => {
    setLoading(true);
    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ login, password }),
      });
      if (!response.ok) {
        showError(await extractError(response));
        return;
      }
      const data = await response.json();
      localStorage.setItem("userName", data.userName);
      localStorage.setItem("userId", data.userId);
      navigate("/lobbies");
    } catch (err) {
      showError(err.message || "Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading };
}
