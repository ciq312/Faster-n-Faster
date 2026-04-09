import { useColorPalette } from "../../hooks/useColorPalette";
import "./LobbyPlayerCard.css";

function LobbyPlayerCard({ player, colors, changeColor, isSelf }) {
  const palette = useColorPalette();

  return (
    <div className="player-card">
      <span className="player-card__nick">{player.nick}</span>
      <div className="player-card__other">
        {player.isHost && <span className="player-card__badge">host</span>}
        {isSelf && <span className="player-card__badge">you</span>}
        <div
          className={
            isSelf
              ? "player-card__color player-card__color--self"
              : "player-card__color"
          }
          style={{ background: player.color }}
          onMouseDown={(e) => {
            e.stopPropagation();
            palette.toggle(isSelf);
          }}
        ></div>
      </div>
      {palette.showColors && colors && (
        <div className="color-panel" ref={palette.panelRef}>
          {colors.map((c, i) => (
            <div
              key={i}
              className={`player-card__color player-card__choose ${!c.isAvailable ? "player-card__color--taken" : ""}`}
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
