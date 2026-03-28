import { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import CreateLobbyModal from "../components/CreateLobbyModal";
import ErrorBanner from "../components/ErrorBanner";
import "./Lobbies.css";

async function extractError(response) {
  try {
    const body = await response.json();
    if (body.errors) {
      const firstKey = Object.keys(body.errors)[0];
      if (firstKey && body.errors[firstKey]?.length) return body.errors[firstKey][0];
    }
    return body.message || `Error ${response.status}`;
  } catch {
    return "Something went wrong, try again";
  }
}

function Lobbies() {
  const [inviteCode, setInviteCode] = useState("");
  const [lobbies, setLobbies] = useState([]);
  const [isPending, setIsPending] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [error, setError] = useState(null);

  const navigate = useNavigate();
  const clearError = useCallback(() => setError(null), []);

  const fetchLobbies = async () => {
    setIsPending(true);
    setLobbies([]);
    setError(null);
    setTimeout(async () => {
      try {
        const response = await fetch("/api/lobbies");
        if (!response.ok) {
          setError(await extractError(response));
          setIsPending(false);
          return;
        }
        const data = await response.json();
        setLobbies(data.lobbies);
      } catch {
        setError("Could not connect to server");
      } finally {
        setIsPending(false);
      }
    }, 500);
  };

  useEffect(() => {
    fetchLobbies();
  }, []);

  const handleCreateLobby = async (lobbyData) => {
    setError(null);
    try {
      const token = localStorage.getItem("token");
      const response = await fetch("/api/lobbies", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(lobbyData),
      });

      if (!response.ok) {
        setError(await extractError(response));
        return;
      }
      const data = await response.json();
      setShowCreateModal(false);
      navigate(`/lobby/${data.lobbyId}`);
    } catch {
      setError("Could not connect to server");
    }
  };

  const handleJoinByCode = (e) => {
    e.preventDefault();
    if (!inviteCode.trim()) return;
  };

  const handleJoinLobby = (lobbyId) => {
    navigate(`/lobby/${lobbyId}`);
  };

  return (
    <div className="lobbies-page">
      <Navbar />

      <ErrorBanner message={error} onDismiss={clearError} />
      <div className="lobbies-page__content">
        <section className="lobby-config">
          <button
            className="lobby-config__create"
            onClick={() => setShowCreateModal(true)}
          >
            create lobby
          </button>

          <button className="lobby-config__create" onClick={fetchLobbies}>
            Refresh
          </button>

          <form className="lobby-config__join" onSubmit={handleJoinByCode}>
            <span className="lobby-config__join-label">
              join via invite code
            </span>
            <div className="lobby-config__join-row">
              <input
                className="lobby-config__input"
                type="text"
                value={inviteCode}
                onChange={(e) => setInviteCode(e.target.value)}
                placeholder="enter code"
              />
              <button
                className="lobby-config__join-btn"
                type="submit"
                disabled={!inviteCode.trim()}
              >
                join lobby
              </button>
            </div>
          </form>
        </section>

        <section className="lobby-list">
          <h2 className="lobby-list__title">Active lobbies</h2>
          {isPending && <div>Loading...</div>}
          {!isPending && lobbies.length === 0 ? (
            <p className="lobby-list__empty">No active lobbies — create one!</p>
          ) : (
            <div className="lobby-list__cards">
              {lobbies.map(
                (lobby) =>
                  !lobby.isPrivate && (
                    <div key={lobby.id} className="lobby-card">
                      <div className="lobby-card__info">
                        <span className="lobby-card__name">{lobby.name}</span>
                        <span className="lobby-card__count">
                          {lobby.playerCount}/{lobby.maxPlayers}
                        </span>
                      </div>
                      <button
                        className="lobby-card__join"
                        onClick={() => handleJoinLobby(lobby.id)}
                      >
                        join lobby
                      </button>
                    </div>
                  ),
              )}
            </div>
          )}
        </section>
      </div>
      {showCreateModal && (
        <CreateLobbyModal
          onClose={() => setShowCreateModal(false)}
          onCreate={handleCreateLobby}
        />
      )}
    </div>
  );
}

export default Lobbies;
