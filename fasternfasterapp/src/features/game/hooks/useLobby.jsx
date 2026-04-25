import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  useBannerMessage,
  useError,
} from "../../../shared/components/BannerProvider";
import { eventBus } from "../../../shared/components/eventBus";
import { useAuth } from "../../auth/AuthContext";
import { useConnection } from "../../connection/ConnectionProvider";
import { useLobbyContext } from "./LobbyProvider";

export function useLobby(lobbyId, inviteCode) {
  const { showError } = useError();
  const { showMessage } = useBannerMessage();
  const { setLobbyId } = useLobbyContext();
  const { invoke, subscribe, isConnected } = useConnection();
  const { userId: selfId } = useAuth();
  const navigate = useNavigate();
  const [isHost, setIsHost] = useState(false);

  const [players, setPlayers] = useState([]);
  const [lobbyName, setLobbyName] = useState(null);
  const [lobbyMaxPlayers, setLobbyMaxPlayers] = useState(null);
  const [lobbyInviteCode, setLobbyInviteCode] = useState(null);
  const [colors, setColors] = useState(null);

  useEffect(() => {
    // Wait for both the SignalR connection and the auth probe — selfId is null
    // while AuthContext is still resolving /api/auth/me on initial load.
    if (!isConnected || !selfId) return;

    const connectToLobby = async () => {
      try {
        console.log(`connecting to lobby`);
        setLobbyId(lobbyId);
        await invoke("ConnectToLobby", lobbyId, inviteCode);
        console.log(`connected to lobby`);
      } catch (e) {
        console.log(e);
        navigate("/lobbies");
      }
    };

    const onLeaveLobby = () => {
      setLobbyId(null);
      navigate("/lobbies");
    };

    eventBus.on("leaveLobby", onLeaveLobby);

    const cleanups = [
      subscribe("LobbyState", (state) => {
        console.log(state);
        setPlayers(state.players);
        setLobbyName(state.lobbyName);
        setLobbyMaxPlayers(state.maxPlayers);
        setLobbyInviteCode(state.inviteCode);
        setColors(state.colors);
        setIsHost(
          state.players.find((el) => el.id === selfId)?.isHost ?? false,
        );
      }),
      subscribe("Kicked", () => {
        leaveLobby();
        showMessage(`You were kicked from lobby`);
      }),
      subscribe("HostChanged", (data) => {
        console.log(data);
        showMessage(`New host is ${data.newHostNick}  `);
      }),
      subscribe("Error", (e) => {
        if (
          e.toLowerCase().includes("not found") ||
          e.toLowerCase().includes("not accepting") ||
          e.toLowerCase().includes("invalid invite code")
        ) {
          showError(e);
          eventBus.emit("leaveLobby", { lobbyId });
        }
      }),
    ];

    connectToLobby();

    return () => {
      cleanups.forEach((clean) => clean());
      eventBus.off("leaveLobby", onLeaveLobby);
    };
  }, [isConnected, selfId]);

  const changeColor = useCallback(async (color) => {
    try {
      await invoke("ChangeColor", color);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const kickPlayer = useCallback(async (targetId) => {
    await invoke("KickPlayer", targetId);
  });

  const transferHost = useCallback(async (targetId) => {
    await invoke("TransferHost", targetId);
  });

  const leaveLobby = useCallback(async () => {
    try {
      await invoke("LeaveLobby");
      eventBus.emit("leaveLobby", { lobbyId });
    } catch (e) {
      console.log(e);
    }
  }, []);

  const isSelf = useCallback((id) => id === selfId, [selfId]);

  return {
    leaveLobby,
    changeColor,
    isSelf,
    selfId,
    isHost,
    players,
    lobbyName,
    kickPlayer,
    transferHost,
    lobbyMaxPlayers,
    lobbyInviteCode,
    colors,
  };
}
