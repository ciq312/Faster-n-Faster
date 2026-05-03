import { useEffect, useState } from "react";
import { useAuth } from "../../auth/AuthContext";
import { useConnection } from "../../connection/ConnectionProvider";

export function useLobby() {
  const { invoke, subscribe, isConnected } = useConnection();
  const [isHost, setIsHost] = useState(false);
  const [players, setPlayers] = useState([]);
  const [lobbyName, setLobbyName] = useState(null);
  const [lobbyMaxPlayers, setLobbyMaxPlayers] = useState(null);
  const [lobbyInviteCode, setLobbyInviteCode] = useState(null);
  const [colors, setColors] = useState(null);
  const { isSelf, userId } = useAuth();

  useEffect(() => {
    // Wait for both the SignalR connection and the auth probe — selfId is null
    // while AuthContext is still resolving /api/auth/me on initial load.
    if (!isConnected) return;

    const renderLobby = async () => {
      await invoke("RefreshLobby");
    };

    const cleanups = [
      subscribe("LobbyState", (state) => {
        setPlayers(state.players);
        setLobbyName(state.lobbyName);
        setLobbyMaxPlayers(state.maxPlayers);
        setLobbyInviteCode(state.inviteCode);
        setColors(state.colors);
        setIsHost(state.players.find((p) => isSelf(p.id)).isHost);
      }),
    ];

    renderLobby();

    return () => {
      cleanups.forEach((clean) => clean());
    };
  }, [isConnected]);

  return {
    isHost,
    players,
    lobbyName,
    lobbyMaxPlayers,
    lobbyInviteCode,
    colors,
  };
}
