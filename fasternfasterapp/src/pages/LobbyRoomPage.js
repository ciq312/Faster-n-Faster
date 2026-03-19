import { useState, useEffect, useRef } from "react";
import { useParams, useLocation, useNavigate } from "react-router-dom";
import connection from "../signalrConnection";
import "./LobbyRoomPage.css";

function LobbyRoomPage() {
  const { lobbyId } = useParams();
  const { state } = useLocation();
  const navigate = useNavigate();
  const playerId = state?.playerId;

  const [lobbyState, setLobbyState] = useState(null);
  const [error, setError] = useState(null);
  const connectedRef = useRef(false);

  useEffect(() => {
    if (!playerId) {
      setError("Missing player identity. Please join from the lobby list.");
      return;
    }

    let cancelled = false;

    async function connect() {
      connection.on("LobbyState", (data) => {
        if (!cancelled) setLobbyState(data);
      });

      connection.on("PlayerConnected", (player) => {
        if (cancelled) return;
        setLobbyState((prev) => {
          if (!prev) return prev;
          const exists = prev.players.some((p) => p.id === player.playerId);
          if (exists) {
            return {
              ...prev,
              players: prev.players.map((p) =>
                p.id === player.playerId ? { ...p, isConnected: true } : p
              ),
            };
          }
          return {
            ...prev,
            players: [
              ...prev.players,
              {
                id: player.playerId,
                displayName: player.displayName,
                joinOrder: player.joinOrder,
                isConnected: true,
              },
            ].sort((a, b) => a.joinOrder - b.joinOrder),
          };
        });
      });

      connection.on("PlayerDisconnected", (data) => {
        if (cancelled) return;
        setLobbyState((prev) => {
          if (!prev) return prev;
          return {
            ...prev,
            players: prev.players.map((p) =>
              p.id === data.playerId ? { ...p, isConnected: false } : p
            ),
          };
        });
      });

      connection.on("HostChanged", (data) => {
        if (cancelled) return;
        setLobbyState((prev) =>
          prev ? { ...prev, hostPlayerId: data.newHostPlayerId } : prev
        );
      });

      connection.on("Error", (msg) => {
        if (!cancelled) setError(msg);
      });

      try {
        if (connection.state === "Disconnected") {
          await connection.start();
        }
        connectedRef.current = true;
        await connection.invoke("ConnectToLobby", lobbyId, playerId);
      } catch (err) {
        if (!cancelled) setError("Failed to connect to lobby.");
        console.error("SignalR connection error:", err);
      }
    }

    connect();

    return () => {
      cancelled = true;
      connection.off("LobbyState");
      connection.off("PlayerConnected");
      connection.off("PlayerDisconnected");
      connection.off("HostChanged");
      connection.off("Error");
      if (connectedRef.current) {
        connection.stop();
        connectedRef.current = false;
      }
    };
  }, [lobbyId, playerId]);

  function handleLeave() {
    navigate("/");
  }

  if (error) {
    return (
      <div className="page lobby-room">
        <p className="lobby-error">{error}</p>
        <button className="btn-ghost" onClick={handleLeave}>
          Back to Lobbies
        </button>
      </div>
    );
  }

  if (!lobbyState) {
    return (
      <div className="page lobby-room">
        <p className="lobby-loading">Connecting...</p>
      </div>
    );
  }

  const isHost = lobbyState.hostPlayerId === playerId;

  return (
    <div className="page lobby-room">
      <div className="page-header">
        <h1 className="page-title">{lobbyState.lobbyName}</h1>
        <span className="lobby-mode">
          {lobbyState.gameMode === "wordcount" ? "Word Count" : "Timer"}
          {lobbyState.isPrivate && " \u00b7 Private"}
        </span>
        <button className="btn-ghost leave-btn" onClick={handleLeave}>
          Leave
        </button>
      </div>

      <ul className="player-list">
        {lobbyState.players.map((p) => (
          <li
            key={p.id}
            className={`player-item ${p.isConnected ? "" : "disconnected"}`}
          >
            <span className="player-dot" />
            <span className="player-display-name">
              {p.displayName}
              {p.id === lobbyState.hostPlayerId && (
                <span className="badge badge-host">Host</span>
              )}
              {p.id === playerId && (
                <span className="badge badge-you">You</span>
              )}
            </span>
          </li>
        ))}
      </ul>

      <p className="lobby-status">
        {isHost
          ? "You are the host. Start race coming soon..."
          : "Waiting for host to start..."}
      </p>
    </div>
  );
}

export default LobbyRoomPage;
