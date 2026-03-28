import "./LobbyPlayerCard.css";
import { useState, useEffect, useRef } from "react";

function LobbyPlayerCard({ player, colors, openPalette, changeColor }) {
  const [showColors, setShowColors] = useState(false);
  const panelRef = useRef(null);

  useEffect(() => {
    if (!showColors) return;
    const handleClick = (e) => {
      if (panelRef.current && !panelRef.current.contains(e.target))
        setShowColors(false);
    };
    document.addEventListener("mousedown", handleClick);
    return () => document.removeEventListener("mousedown", handleClick);
  }, [showColors]);

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
            setShowColors(showColors ? false : openPalette(player.id));
          }}
        ></div>
      </div>
      {showColors && (
        <div className="color-panel" ref={panelRef}>
          {colors.map((c, i) => (
            <div
              key={i}
              className={`player-card__color ${!c.isAvailable ? "player-card__color--taken" : ""}`}
              style={{ backgroundColor: c.color }}
              onClick={() => {
                if (c.isAvailable) {
                  changeColor(c.color);
                  setShowColors(false);
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
