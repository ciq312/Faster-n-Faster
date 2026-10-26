import { useEffect, useRef, useState } from "react";
import { useColorPalette } from "../../hooks/useColorPalette";
import type {
  ColorStatusDTO,
  LobbyPlayerDTO,
} from "../../../../../generated/signalr/FasterNFaster.Api.UseCases.Lobbies.LobbyState";
import "./LobbyPlayerCard.css";

type LobbyPlayerCardProps = {
  player: LobbyPlayerDTO;
  colors?: ColorStatusDTO[];
  changeColor?: (color: string) => void;
  kickPlayer?: (targetId: string) => void;
  transferHost?: (targetId: string) => void;
  isSelf?: boolean;
  isHost?: boolean;
};

function LobbyPlayerCard({
  player,
  colors,
  changeColor,
  kickPlayer,
  transferHost,
  isSelf = false,
  isHost = false,
}: LobbyPlayerCardProps) {
  const palette = useColorPalette();
  const cardRef = useRef<HTMLDivElement>(null);
  const [showHostActions, setShowHostActions] = useState(false);

  useEffect(() => {
    if (!showHostActions) return;

    const handleClick = (e: MouseEvent) => {
      if (
        cardRef.current &&
        !cardRef.current.contains(e.target as Node | null)
      ) {
        setShowHostActions(false);
      }
    };

    document.addEventListener("mousedown", handleClick);

    return () => document.removeEventListener("mousedown", handleClick);
  }, [showHostActions]);

  return (
    <div
      className="player-card"
      ref={cardRef}
      onMouseDown={(e) => {
        e.stopPropagation();
        if (isHost && !isSelf) {
          setShowHostActions((prev) => !prev);
        }
      }}
    >
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
        />
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
            />
          ))}
        </div>
      )}
      {showHostActions && (
        <div className="host-actions" onMouseDown={(e) => e.stopPropagation()}>
          <button
            className="host-actions__btn"
            onClick={() => {
              transferHost?.(player.id);
              setShowHostActions(false);
            }}
          >
            promote
          </button>
          <button
            className="host-actions__btn host-actions__btn--kick"
            onClick={() => {
              kickPlayer?.(player.id);
              setShowHostActions(false);
            }}
          >
            kick player
          </button>
        </div>
      )}
    </div>
  );
}

export default LobbyPlayerCard;
