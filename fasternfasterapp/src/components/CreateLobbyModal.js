import { useState } from "react";
import "./CreateLobbyModal.css";

function CreateLobbyModal({ onClose, onCreate }) {
  const [lobbyName, setLobbyName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);

  const canCreate = lobbyName.trim().length > 0;

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!canCreate) return;

    onCreate({
      lobbyName: lobbyName.trim(),
      isPrivate,
      gameMode: "wordcount",
      wordCount: 50,
      timerDurationSeconds: null,
    });
  };

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal__header">
          <h2 className="modal__title">Create lobby</h2>
          <button className="modal__close" onClick={onClose} type="button">
            &times;
          </button>
        </div>

        <form className="modal__body" onSubmit={handleSubmit}>
          {/* Lobby name */}
          <label className="modal__field">
            <span className="modal__label">lobby name</span>
            <input
              className="modal__input"
              type="text"
              value={lobbyName}
              onChange={(e) => setLobbyName(e.target.value)}
              placeholder="name your lobby"
              maxLength={30}
              autoFocus
            />
          </label>

          {/* Private toggle */}
          <div className="modal__field">
            <span className="modal__label">visibility</span>
            <div className="modal__toggle-row">
              <button
                type="button"
                className={`modal__toggle-option ${!isPrivate ? "modal__toggle-option--active" : ""}`}
                onClick={() => setIsPrivate(false)}
              >
                public
              </button>
              <button
                type="button"
                className={`modal__toggle-option ${isPrivate ? "modal__toggle-option--active" : ""}`}
                onClick={() => setIsPrivate(true)}
              >
                private
              </button>
            </div>
          </div>

          <button
            className="modal__submit"
            type="submit"
            disabled={!canCreate}
          >
            create
          </button>
        </form>
      </div>
    </div>
  );
}

export default CreateLobbyModal;
