import { useState, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { useFetchLobbies } from "../features/lobbies/hooks/useFetchLobbies";
import { useCreateLobby } from "../features/lobbies/hooks/useCreateLobby";
import Navbar from "../shared/components/Navbar";
import CreateLobbyModal from "../features/lobbies/components/CreateLobbyModal";
import ErrorBanner from "../shared/components/ErrorBanner";
import "./Lobbies.css";
import { useLobbyContext } from "../features/game/hooks/LobbyProvider";

function Lobbies() {
  const { lobbyId } = useLobbyContext();
  const [inviteCode, setInviteCode] = useState("");
  const [showCreateModal, setShowCreateModal] = useState(false);

  const navigate = useNavigate();
  const fetchLobbies = useFetchLobbies();
  const createLobby = useCreateLobby();

  const error = fetchLobbies.error || createLobby.error;

  const clearError = useCallback(() => {
    fetchLobbies.setError(null);
    createLobby.setError(null);
  }, [fetchLobbies, createLobby]);

  const handleCreate = async (lobbyData) => {
    const success = await createLobby.execute(lobbyData);
    if (success) setShowCreateModal(false);
  };

  const handleJoinByCode = async (e) => {
    e.preventDefault();
    const code = inviteCode.trim();
    if (!code) return;

    try {
      const token = localStorage.getItem("token");
      const response = await fetch(`/api/lobbies/by-code/${encodeURIComponent(code)}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!response.ok) {
        fetchLobbies.setError("Invalid invite code.");
        return;
      }

      const data = await response.json();
      navigate(`/lobby/${data.lobbyId}`, { state: { inviteCode: code } });
    } catch {
      fetchLobbies.setError("Could not connect to server.");
    }
  };

  return (
    <div className="lobbies-page">
      <Navbar />
      {lobbyId && (
        <button
          className="return-to-lobby-btn"
          onClick={() => navigate(`/lobby/${lobbyId}`)}
        >
          Back to lobby
        </button>
      )}
      <ErrorBanner message={error} onDismiss={clearError} />
      <div className="lobbies-page__content">
        <section className="lobby-config">
          <button
            className="lobby-config__create"
            onClick={() => setShowCreateModal(true)}
          >
            create lobby
          </button>

          <button
            className="lobby-config__create"
            onClick={fetchLobbies.refresh}
          >
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
          {fetchLobbies.loading && <div>Loading...</div>}
          {!fetchLobbies.loading && fetchLobbies.lobbies.length === 0 ? (
            <p className="lobby-list__empty">No active lobbies — create one!</p>
          ) : (
            <div className="lobby-list__cards">
              {fetchLobbies.lobbies.map(
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
                        onClick={() => navigate(`/lobby/${lobby.id}`)}
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
          onCreate={handleCreate}
        />
      )}
    </div>
  );
}

export default Lobbies;
