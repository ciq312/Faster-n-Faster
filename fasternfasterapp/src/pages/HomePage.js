import { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { getLobbies, joinLobby } from "../api/lobbyApi";
import LobbyList from "../components/LobbyList";
import "./HomePage.css";

function HomePage({ player }) {
  const [lobbies, setLobbies] = useState([]);
  const navigate = useNavigate();

  const fetchLobbies = useCallback(async () => {
    try {
      const data = await getLobbies();
      setLobbies(data);
    } catch (err) {
      console.error("Failed to fetch lobbies:", err);
    }
  }, []);

  useEffect(() => {
    fetchLobbies();
  }, [fetchLobbies]);

  async function handleJoinLobby(lobbyId) {
    try {
      const data = await joinLobby(lobbyId, {
        displayName: player.displayName,
        playerId: player.playerId,
      });
      navigate(`/lobby/${data.lobbyId}`, {
        state: { playerId: data.playerId },
      });
    } catch (err) {
      console.error("Failed to join lobby:", err);
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1 className="page-title">Faster'n'Faster</h1>
        <span className="player-name">{player.displayName}</span>
        <button className="btn-primary" onClick={() => navigate("/create")}>
          + Create Lobby
        </button>
      </div>

      <button className="btn-ghost refresh-btn" onClick={fetchLobbies}>
        Refresh
      </button>

      <LobbyList lobbies={lobbies} onJoin={handleJoinLobby} />
    </div>
  );
}

export default HomePage;
