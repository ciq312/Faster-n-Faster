import { useCallback, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useBannerMessage, useError } from "../../../shared/components/BannerProvider";
import { ROUTES } from "../../../shared/utils/routes";
import { useConnection } from "../../connection/ConnectionProvider";
import { useLobbyContext } from "./LobbyProvider";

export function useLobbyActions() {
  const { invoke, subscribe, isConnected } = useConnection();
  const { showMessage } = useBannerMessage();
  const { setLobbyId } = useLobbyContext();
  const { showError } = useError();
  const navigate = useNavigate();

  const lobbyCleanup = useCallback(() => {
    setLobbyId(null);
  });

  useEffect(() => {
    const cleanups = [
      subscribe("PlayerKicked", (kickedDTO) => {
        showMessage(`player ${kickedDTO.nick} was kicked from lobby`);
      }),
      subscribe("PlayerDisconnected", (diconnectedDTO) => {
        showMessage(
          `player ${diconnectedDTO.disconnectedUserNick} disconnected from lobby`,
        );
      }),
      subscribe("Kicked", () => {
        showMessage(`You were kicked from lobby`);
        lobbyCleanup();
        navigate(ROUTES.LOBBIES);
      }),
      subscribe("HostChanged", (data) => {
        showMessage(`New host is ${data.newHostNick}`);
      }),
    ];
    return () => cleanups.forEach((fn) => fn());
  }, [isConnected]);

  const changeColor = useCallback(async (color) => {
    await invoke("ChangeColor", color);
  }, []);

  const kickPlayer = useCallback(async (targetId) => {
    await invoke("KickPlayer", targetId);
  }, []);

  const transferHost = useCallback(async (targetId) => {
    await invoke("TransferHost", targetId);
  }, []);

  const leaveLobby = useCallback(async () => {
    await invoke("LeaveLobby");
    lobbyCleanup();
    navigate(ROUTES.LOBBIES);
  }, []);

  return {
    changeColor,
    kickPlayer,
    transferHost,
    leaveLobby,
  };
}
