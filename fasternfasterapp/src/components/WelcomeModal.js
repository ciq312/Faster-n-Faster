import { createAnonymousPlayer } from "../playerIdentity";
import "./WelcomeModal.css";

function WelcomeModal({ onLogin }) {
  function handleAnonymous() {
    const player = createAnonymousPlayer();
    onLogin(player);
  }

  return (
    <div className="modal-backdrop">
      <div className="modal welcome-modal">
        <h2 className="welcome-title">Faster'n'Faster</h2>
        <p className="welcome-subtitle">Real-time multiplayer typing races</p>

        <div className="welcome-actions">
          <button className="btn-primary welcome-btn" onClick={handleAnonymous}>
            Continue Anonymously
          </button>
          <button className="btn-ghost welcome-btn" disabled>
            Register
            <span className="coming-soon">Coming soon</span>
          </button>
        </div>
      </div>
    </div>
  );
}

export default WelcomeModal;
