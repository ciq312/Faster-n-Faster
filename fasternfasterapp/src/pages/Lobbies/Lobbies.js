import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import CreateLobbyModal from "../../features/lobbies/components/CreateLobbyModal";
import { useCreateLobby } from "../../features/lobbies/hooks/useCreateLobby";
import { useFetchLobbies } from "../../features/lobbies/hooks/useFetchLobbies";
import { useError } from "../../shared/components/BannerProvider";
import Navbar from "../../shared/components/Navbar/Navbar";
import "./Lobbies.css";

function Lobbies() {
  const [inviteCode, setInviteCode] = useState("");
  const [showCreateModal, setShowCreateModal] = useState(false);
  const navigate = useNavigate();
  const fetchLobbies = useFetchLobbies();
  const createLobby = useCreateLobby();
  const { showError } = useError();

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
      const response = await fetch(
        `/api/lobbies/by-code/${encodeURIComponent(code)}`,
        {
          headers: { Authorization: `Bearer ${token}` },
        },
      );

      if (!response.ok) {
        showError("Invalid invite code");
        return;
      }

      const data = await response.json();
      navigate(`/lobby/${data.lobbyId}`, { state: { inviteCode: code } });
    } catch {
      showError("Could not connect to server.");
    }
  };

  return (
    <div className="lobbies-page">
      <Navbar />
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
