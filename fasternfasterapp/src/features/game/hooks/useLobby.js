import { useEffect, useEffectEvent, useState } from "react";
import { useAppSelector } from "../../../app/hooks";
import { selectAuthStatus, selectIsSelf } from "../../auth/authSelectors";
import { useConnection } from "../../connection/ConnectionProvider";

export function useLobby() {
  const { invoke, subscribe, isConnected } = useConnection();
  const [isHost, setIsHost] = useState(false);
  const [players, setPlayers] = useState([]);
  const [lobbyName, setLobbyName] = useState(null);
  const [lobbyMaxPlayers, setLobbyMaxPlayers] = useState(null);
  const [lobbyInviteCode, setLobbyInviteCode] = useState(null);
  const [colors, setColors] = useState(null);
  const isSelf = useAppSelector(selectIsSelf);
  const status = useAppSelector(selectAuthStatus);
  const isAuthenticated = status === "authenticated";

  const onLobbyState = useEffectEvent((state) => {
    const self = state.players.find((p) => isSelf(p.id));
    if (!self) return;
    setPlayers(state.players);
    setLobbyName(state.lobbyName);
    setLobbyMaxPlayers(state.maxPlayers);
    setLobbyInviteCode(state.inviteCode);
    setColors(state.colors);
    setIsHost(self.isHost);
  });

  useEffect(() => {
    // LobbyState is filtered by isSelf, so wait until /api/auth/me has
    // resolved the user before subscribing and refreshing.
    if (!isConnected || !isAuthenticated) return;

    const renderLobby = async () => {
      await invoke("RefreshLobby");
    };

    const cleanups = [subscribe("LobbyState", (state) => onLobbyState(state))];

    renderLobby();

    return () => {
      cleanups.forEach((clean) => clean());
    };
  }, [isConnected, isAuthenticated, invoke, subscribe]);

  return {
    isHost,
    players,
    lobbyName,
    lobbyMaxPlayers,
    lobbyInviteCode,
    colors,
  };
}
