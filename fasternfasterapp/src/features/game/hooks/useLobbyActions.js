import { useCallback, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useBannerMessage } from "../../../shared/components/BannerProvider";
import { ROUTES } from "../../../shared/utils/routes";
import { useConnection } from "../../connection/ConnectionProvider";
import { useLobbyContext } from "./LobbyProvider";

export function useLobbyActions() {
  const { invoke, invokeOrShowError, subscribe, isConnected } = useConnection();
  const { showMessage } = useBannerMessage();
  const { setLobbyId } = useLobbyContext();
  const navigate = useNavigate();

  const lobbyCleanup = useCallback(() => {
    setLobbyId(null);
  }, [setLobbyId]);

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
  }, [isConnected, subscribe, showMessage, lobbyCleanup, navigate]);

  const changeColor = useCallback(
    async (color) => {
      await invokeOrShowError("ChangeColor", color);
    },
    [invokeOrShowError],
  );

  const kickPlayer = useCallback(
    async (targetId) => {
      await invokeOrShowError("KickPlayer", targetId);
    },
    [invokeOrShowError],
  );

  const transferHost = useCallback(
    async (targetId) => {
      await invokeOrShowError("TransferHost", targetId);
    },
    [invokeOrShowError],
  );

  const leaveLobby = useCallback(async () => {
    await invoke("LeaveLobby");
    lobbyCleanup();
    navigate(ROUTES.LOBBIES);
  }, [invoke, navigate, lobbyCleanup]);

  return {
    changeColor,
    kickPlayer,
    transferHost,
    leaveLobby,
  };
}
