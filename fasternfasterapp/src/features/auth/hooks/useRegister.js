import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { extractError } from "../../../shared/utils/extractError";
import { useLogin } from "./useLogin";

export function useRegister() {
  const { showError } = useError();
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const { execute: loginUser } = useLogin();

  const execute = async ({ nick, login, password }) => {
    setLoading(true);
    try {
      const response = await fetch("/api/auth/register", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ nick, login, password }),
      });
      if (!response.ok) {
        showError(await extractError(response));
        return;
      }
      await loginUser({ login, password });

      navigate("/lobbies");
    } catch (err) {
      showError(err.message || "Could not connect to server");
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading };
}
