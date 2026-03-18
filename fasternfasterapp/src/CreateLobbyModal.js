import { useState } from "react";
import connection from "./signalrConnection";
import "./CreateLobbyModal.css";

const GAME_MODES = {
  WordCount: "WordCount",
  Timer: "Timer",
};

const defaultForm = {
  lobbyName: "",
  displayName: "",
  gameMode: GAME_MODES.WordCount,
  isPrivate: false,
  wordCount: 50,
  timerDurationSeconds: 60,
};

function CreateLobbyModal({ onClose }) {
  const [form, setForm] = useState(defaultForm);

  function handleChange(e) {
    const { name, value, type, checked } = e.target;
    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  }

  async function handleSubmit(e) {
    e.preventDefault();

    const payload = {
      lobbyName: form.lobbyName,
      displayName: form.displayName,
      gameMode: form.gameMode,
      isPrivate: form.isPrivate,
      wordCount:
        form.gameMode === GAME_MODES.WordCount ? Number(form.wordCount) : null,
      timerDurationSeconds:
        form.gameMode === GAME_MODES.Timer
          ? Number(form.timerDurationSeconds)
          : null,
    };

    try {
      const res = await fetch("/api/lobbies", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      if (!res.ok) throw new Error(await res.text());

      onClose();
    } catch (err) {
      console.error("Failed to create lobby:", err);
    }
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Create Lobby</h2>
          <button className="modal-close" onClick={onClose} aria-label="Close">
            ✕
          </button>
        </div>

        <form className="modal-form" onSubmit={handleSubmit}>
          <label>
            Lobby Name
            <input
              name="lobbyName"
              value={form.lobbyName}
              onChange={handleChange}
              placeholder="My Race Lobby"
              maxLength={40}
              required
            />
          </label>

          <label>
            Your Display Name
            <input
              name="displayName"
              value={form.displayName}
              onChange={handleChange}
              placeholder="SpeedTyper42"
              maxLength={24}
              required
            />
          </label>

          <div className="form-row">
            <span className="form-label">Game Mode</span>
            <div className="toggle-group">
              {Object.values(GAME_MODES).map((mode) => (
                <button
                  key={mode}
                  type="button"
                  className={`toggle-btn ${form.gameMode === mode ? "active" : ""}`}
                  onClick={() =>
                    setForm((prev) => ({ ...prev, gameMode: mode }))
                  }
                >
                  {mode === GAME_MODES.WordCount ? "Word Count" : "Timer"}
                </button>
              ))}
            </div>
          </div>

          {form.gameMode === GAME_MODES.WordCount && (
            <label>
              Word Count
              <input
                name="wordCount"
                type="number"
                min={10}
                max={500}
                value={form.wordCount}
                onChange={handleChange}
                required
              />
            </label>
          )}

          {form.gameMode === GAME_MODES.Timer && (
            <label>
              Timer (seconds)
              <input
                name="timerDurationSeconds"
                type="number"
                min={15}
                max={300}
                value={form.timerDurationSeconds}
                onChange={handleChange}
                required
              />
            </label>
          )}

          <label className="checkbox-label">
            <input
              name="isPrivate"
              type="checkbox"
              checked={form.isPrivate}
              onChange={handleChange}
            />
            Private lobby
          </label>

          <button type="submit" className="submit-btn">
            Create
          </button>
        </form>
      </div>
    </div>
  );
}

export default CreateLobbyModal;
