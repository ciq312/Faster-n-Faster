import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useError } from "../../../shared/components/BannerProvider";
import { eventBus } from "../../../shared/components/eventBus";
import { useConnection } from "../../connection/ConnectionProvider";
import { useLobbyContext } from "./LobbyProvider";

export function useLobby(lobbyId, inviteCode) {
  const { showError } = useError();
  const { setLobbyId } = useLobbyContext();
  const { invoke, subscribe, isConnected } = useConnection();
  const selfIdRef = useRef(localStorage.getItem("userId"));
  const navigate = useNavigate();
  const isHostRef = useRef(false);

  const [players, setPlayers] = useState([]);
  const [lobbyName, setLobbyName] = useState(null);
  const [lobbyMaxPlayers, setLobbyMaxPlayers] = useState(null);
  const [lobbyInviteCode, setLobbyInviteCode] = useState(null);
  const [colors, setColors] = useState(null);

  useEffect(() => {
    if (!isConnected) return;

    const connectToLobby = async () => {
      try {
        console.log(`connecting to lobby`);
        setLobbyId(lobbyId);
        await invoke("ConnectToLobby", lobbyId, inviteCode);
        console.log(`connected to lobby`);
      } catch (e) {
        console.log(e);
      }
    };

    const onLeaveLobby = () => {
      setLobbyId(null);
      navigate("/lobbies");
    };

    eventBus.on("leaveLobby", onLeaveLobby);

    const cleanups = [
      subscribe("LobbyState", (state) => {
        console.log("LobbyState:", state);
        setPlayers(state.players);
        setLobbyName(state.lobbyName);
        setLobbyMaxPlayers(state.maxPlayers);
        setLobbyInviteCode(state.inviteCode);
        setColors(state.colors);
        isHostRef.current = state.players.find(
          (el) => el.id === selfIdRef.current,
        ).isHost;
      }),

      subscribe("Error", (e) => {
        if (
          e.toLowerCfpase().includes("not found") ||
          e.toLowerCase().includes("not accepting")
        ) {
          eventBus.emit("leaveLobby", { lobbyId });
        }
      }),
    ];

    connectToLobby();

    return () => {
      cleanups.forEach((clean) => clean());
      eventBus.off("leaveLobby", onLeaveLobby);
    };
  }, [isConnected]);

  const changeColor = useCallback(async (color) => {
    try {
      await invoke("ChangeColor", color);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const leaveLobby = useCallback(async () => {
    try {
      await invoke("LeaveLobby");
      eventBus.emit("leaveLobby", { lobbyId });
    } catch (e) {
      console.log(e);
    }
  }, []);

  const isSelf = useCallback((id) => id === selfIdRef.current, []);

  return {
    leaveLobby,
    changeColor,
    isSelf,
    selfId: selfIdRef.current,
    isHost: isHostRef.current,
    players,
    lobbyName,
    lobbyMaxPlayers,
    lobbyInviteCode,
    colors,
  };
}
