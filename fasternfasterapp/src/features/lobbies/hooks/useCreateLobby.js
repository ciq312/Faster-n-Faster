import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { extractError } from "../../../shared/utils/extractError";
import { useError } from "../../../shared/components/BannerProvider";

export function useCreateLobby() {
  const { showError } = useError();
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const execute = async (lobbyData) => {
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
        showError(await extractError(response));
        return false;
      }
      const data = await response.json();
      navigate(`/lobby/${data.lobbyId}`, { state: { inviteCode: data.inviteCode } });
      return true;
    } catch {
      showError("Could not connect to server");
      return false;
    } finally {
      setLoading(false);
    }
  };

  return { execute, loading };
}
