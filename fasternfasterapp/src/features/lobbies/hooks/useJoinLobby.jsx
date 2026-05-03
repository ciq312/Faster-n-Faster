import { useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { extractHubError } from "../../../shared/utils/extractHubError";
import { useConnection } from "../../connection/ConnectionProvider";
import { useLobbyContext } from "../../game/hooks/LobbyProvider";

export function useJoinLobby() {
    const {invoke, subsribe, isConnected} = useConnection();
    const navigate = useNavigate();
    const { showError } = useError();
    const { setLobbyId } = useLobbyContext();

    const joinLobby = useCallback(async (lobbyId,  inviteCode = null) => {
        if (!isConnected) {
            showError("Not connected");
            return;
        } 
        try {
            await invoke("ConnectToLobby", lobbyId,  inviteCode);
            setLobbyId(lobbyId);
            navigate(`/lobby/${lobbyId}`);
        }
        catch (e) {
            showError(extractHubError(e));
        }
    }, [isConnected]);

    return { joinLobby };
}   