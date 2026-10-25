import { useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useAppDispatch } from "../../../app/hooks";
import { showError } from "../../../shared/banner/bannerSlice";
import { extractHubError } from "../../../shared/utils/extractHubError";
import { useConnection } from "../../connection/ConnectionProvider";
import { useLobbyContext } from "../../game/hooks/LobbyProvider";

export function useJoinLobby() {
  const dispatch = useAppDispatch();
  const { invoke } = useConnection();
  const navigate = useNavigate();
  const { setLobbyId } = useLobbyContext();

  const joinLobby = useCallback(
    /**
     * @param {string | null | undefined} [inviteCode]
     */
    async (lobbyId, inviteCode = null) => {
      try {
        await invoke("ConnectToLobby", lobbyId, inviteCode);
        setLobbyId(lobbyId);
        navigate(`/lobby/${lobbyId}`);
      } catch (e) {
        dispatch(showError(extractHubError(e)));
      }
    },
    [invoke, navigate, setLobbyId, dispatch],
  );

  return { joinLobby };
}
