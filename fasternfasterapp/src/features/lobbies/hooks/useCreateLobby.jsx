import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { apiCall } from "../../../shared/utils/apiCall";
import { extractHttpError } from "../../../shared/utils/extractHttpError";
import { useJoinLobby } from "./useJoinLobby";

export function useCreateLobby() {
  const { showError } = useError();
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const {joinLobby} = useJoinLobby();

  const execute = async (lobbyData) => {
    setLoading(true);
    try {
      const response = await apiCall("/api/lobbies", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(lobbyData),
      });

      if (!response.ok) {
        showError(await extractHttpError(response));
        return false;
      }
      const data = await response.json();
      await joinLobby(data.lobbyId, data.inviteCode);
      return true;
    } catch (e){
      showError(e.message);
      return false;
    } finally {
      setLoading(false);
    }

  };

  return { execute, loading };
}
