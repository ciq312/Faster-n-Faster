import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { createLobby } from "../api/lobbyApi";
import "./CreateLobbyPage.css";

const GAME_MODES = {
  WordCount: "WordCount",
  Timer: "Timer",
};

const defaultForm = {
  lobbyName: "",
  gameMode: GAME_MODES.WordCount,
  isPrivate: false,
  wordCount: 50,
  timerDurationSeconds: 60,
};

function CreateLobbyPage({ player }) {
  const [form, setForm] = useState(defaultForm);
  const navigate = useNavigate();

  function handleChange(e) {
    const { name, value, type, checked } = e.target;
    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  }

  async function handleSubmit(e) {
    e.preventDefault();

    try {
      const data = await createLobby({
        lobbyName: form.lobbyName,
        displayName: player.displayName,
        playerId: player.playerId,
        gameMode: form.gameMode,
        isPrivate: form.isPrivate,
        wordCount:
          form.gameMode === GAME_MODES.WordCount ? Number(form.wordCount) : null,
        timerDurationSeconds:
          form.gameMode === GAME_MODES.Timer
            ? Number(form.timerDurationSeconds)
            : null,
      });

      navigate(`/lobby/${data.lobbyId}`, {
        state: { playerId: data.hostPlayerId },
      });
    } catch (err) {
      console.error("Failed to create lobby:", err);
    }
  }

  return (
    <div className="page">
      <div className="page-header">
        <Link to="/" className="back-link">&larr;</Link>
        <h1 className="page-title">Create Lobby</h1>
      </div>

      <form className="create-form" onSubmit={handleSubmit}>
        <label className="form-field">
          Lobby Name
          <input
            className="form-input"
            name="lobbyName"
            value={form.lobbyName}
            onChange={handleChange}
            placeholder="My Race Lobby"
            maxLength={40}
            required
          />
        </label>

        <div className="form-field">
          <span>Game Mode</span>
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
          <label className="form-field">
            Word Count
            <input
              className="form-input"
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
          <label className="form-field">
            Timer (seconds)
            <input
              className="form-input"
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

        <button type="submit" className="btn-primary submit-btn">
          Create
        </button>
      </form>
    </div>
  );
}

export default CreateLobbyPage;
