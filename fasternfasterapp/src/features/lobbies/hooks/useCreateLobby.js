import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { extractError } from "../../../shared/utils/extractError";

export function useCreateLobby() {
  const [error, setError] = useState(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const execute = async (lobbyData) => {
    setError(null);
    setLoading(true);
    try {
      const token = localStorage.getItem("token");
      const response = await fetch("/api/lobbies", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(lobbyData),
      });

      if (!response.ok) {
        setError(await extractError(response));
        return false;
      }
      const data = await response.json();
      navigate(`/lobby/${data.lobbyId}`);
      return true;
    } catch {
      setError("Could not connect to server");
      return false;
    } finally {
      setLoading(false);
    }
  };

  return { execute, error, loading, setError };
}
