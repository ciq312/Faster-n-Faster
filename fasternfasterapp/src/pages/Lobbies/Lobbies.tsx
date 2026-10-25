import { useState, type FormEvent } from "react";
import type { CreateLobbyRequest } from "../../../types";
import CreateLobbyModal from "../../features/lobbies/components/CreateLobbyModal";
import { useJoinLobby } from "../../features/lobbies/hooks/useJoinLobby";
import {
  useCreateLobbyMutation,
  useGetLobbiesQuery,
  useLazyFindLobbyByInviteCodeQuery,
} from "../../features/lobbies/lobbiesApi";
import { errorMessage } from "../../shared/api/errorMessage";
import { useError } from "../../shared/components/BannerProvider";
import Navbar from "../../shared/components/Navbar/Navbar";
import { useQueryErrorBanner } from "../../shared/hooks/useQueryErrorBanner";
import "./Lobbies.css";

function Lobbies() {
  const [inviteCode, setInviteCode] = useState("");
  const [showCreateModal, setShowCreateModal] = useState(false);
  const { joinLobby } = useJoinLobby();
  const {
    data: lobbies = [],
    isLoading,
    isFetching,
    isSuccess,
    error,
    refetch,
  } = useGetLobbiesQuery(undefined, { refetchOnMountOrArgChange: true });
  const [createLobby] = useCreateLobbyMutation();
  const [findLobbyByInviteCode] = useLazyFindLobbyByInviteCodeQuery();
  const { showError } = useError();
  useQueryErrorBanner(error);

  const publicLobbies = lobbies.filter((lobby) => !lobby.isPrivate);

  const handleCreate = async (lobby: CreateLobbyRequest) => {
    const result = await createLobby(lobby);
    if (result.error) {
      showError(errorMessage(result.error));
      return;
    }
    await joinLobby(result.data.lobbyId, result.data.inviteCode);
    setShowCreateModal(false);
  };

  const handleJoinByCode = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const code = inviteCode.trim();
    if (!code) return;

    const result = await findLobbyByInviteCode(code);
    if (result.error) {
      showError(errorMessage(result.error));
      return;
    }
    if (!result.data) {
      showError("Invalid invite code");
      return;
    }
    joinLobby(result.data.id, code);
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

          <button className="lobby-config__create" onClick={() => refetch()}>
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
          {isLoading && <div>Loading...</div>}
          {isSuccess && publicLobbies.length === 0 && (
            <p className="lobby-list__empty">No active lobbies — create one!</p>
          )}
          {publicLobbies.length > 0 && (
            <div className="lobby-list__cards" aria-busy={isFetching}>
              {publicLobbies.map((lobby) => (
                <div key={lobby.id} className="lobby-card">
                  <div className="lobby-card__info">
                    <span className="lobby-card__name">{lobby.name}</span>
                    <span className="lobby-card__count">
                      {lobby.playerCount}/{lobby.maxPlayers}
                    </span>
                  </div>
                  <button
                    className="lobby-card__join"
                    onClick={() => joinLobby(lobby.id)}
                  >
                    join lobby
                  </button>
                </div>
              ))}
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
