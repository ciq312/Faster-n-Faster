import { useConnection } from "../../connection/ConnectionProvider";
import { useState, useRef, useCallback, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useLobbyContext } from "./LobbyProvider";

export function useLobby(lobbyId) {
  const { setLobbyId } = useLobbyContext();
  const { invoke, subscribe } = useConnection();
  const selfIdRef = useRef(localStorage.getItem("userId"));
  const navigate = useNavigate();

  const [players, setPlayers] = useState([]);
  const [lobbyName, setLobbyName] = useState(null);
  const [lobbyMaxPlayers, setLobbyMaxPlayers] = useState(null);
  const [colors, setColors] = useState(null);

  useEffect(() => {
    const connectToLobby = async () => {
      try {
        await invoke("ConnectToLobby", lobbyId);
        setLobbyId(lobbyId);
      } catch (e) {
        console.log(e);
      }
    };

    connectToLobby();

    const cleanups = [
      subscribe("LobbyState", (state) => {
        console.log("LobbyState:", state);
        setPlayers(state.players);
        setLobbyName(state.lobbyName);
        setLobbyMaxPlayers(state.maxPlayers);
        setColors(state.colors);
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
      setLobbyId(null);
      navigate("/lobbies");
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
    players,
    lobbyName,
    lobbyMaxPlayers,
    colors,
  };
}
