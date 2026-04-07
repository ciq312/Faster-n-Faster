import { useConnection } from "../../connection/ConnectionProvider";
import { useState, useRef, useCallback, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useLobbyContext } from "./LobbyProvider";
import { useError } from "../../../shared/components/ErrorProvider";
import { eventBus } from "../../../shared/components/eventBus";

export function useLobby(lobbyId, inviteCode) {
  const { showError } = useError();
  const { setLobbyId } = useLobbyContext();
  const { invoke, subscribe } = useConnection();
  const selfIdRef = useRef(localStorage.getItem("userId"));
  const navigate = useNavigate();
  const isHostRef = useRef(false);

  const [players, setPlayers] = useState([]);
  const [lobbyName, setLobbyName] = useState(null);
  const [lobbyMaxPlayers, setLobbyMaxPlayers] = useState(null);
  const [lobbyInviteCode, setLobbyInviteCode] = useState(null);
  const [colors, setColors] = useState(null);

  useEffect(() => {
    const connectToLobby = async () => {
      try {
        setLobbyId(lobbyId);
        await invoke("ConnectToLobby", lobbyId, inviteCode);
      } catch (e) {
        console.log(e);
      }
    };

    connectToLobby();

    eventBus.on("leaveLobby", (data) => setLobbyId(null));
    eventBus.on("leaveLobby", (data) => navigate("/lobbies"));

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
          e.toLowerCase().includes("not found") ||
          e.toLowerCase().includes("not accepting")
        ) {
          eventBus.emit("leaveLobby", { lobbyId });
        }
        showError(e);
      }),
    ];

    return () => cleanups.map((clean) => clean());
  }, []);

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
