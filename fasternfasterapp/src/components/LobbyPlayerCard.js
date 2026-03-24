import "./LobbyPlayerCard.css";

function LobbyPlayerCard({ player }) {
  return (
    <div className="player-card">
      <span className="player-card__nick">{player.nick}</span>
      {player.isHost && <span className="player-card__badge">host</span>}
    </div>
  );
}

export default LobbyPlayerCard;
