import { useColorPalette } from "../hooks/useColorPalette";
import "./LobbyPlayerCard.css";

function LobbyPlayerCard({ player, colors, openPalette, changeColor }) {
  const palette = useColorPalette();

  return (
    <div className="player-card">
      <span className="player-card__nick">{player.nick}</span>
      <div className="player-card__other">
        {player.isHost && <span className="player-card__badge">host</span>}
        <div
          className="player-card__color"
          style={{ background: player.color }}
          onMouseDown={(e) => {
            e.stopPropagation();
            palette.toggle(openPalette?.(player.id));
          }}
        ></div>
      </div>
      {palette.showColors && colors && (
        <div className="color-panel" ref={palette.panelRef}>
          {colors.map((c, i) => (
            <div
              key={i}
              className={`player-card__color ${!c.isAvailable ? "player-card__color--taken" : ""}`}
              style={{ backgroundColor: c.color }}
              onClick={() => {
                if (c.isAvailable) {
                  changeColor?.(c.color);
                  palette.close();
                }
              }}
            ></div>
          ))}
        </div>
      )}
    </div>
  );
}

export default LobbyPlayerCard;
