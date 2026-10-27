import { pageItems } from "../pageItems";
import "./Pager.css";

type PagerProps = {
  current: number;
  total: number;
  onChange: (page: number) => void;
};

function Pager({ current, total, onChange }: PagerProps) {
  return (
    <nav className="leaderboard-pager" aria-label="Leaderboard pages">
      <button
        className="leaderboard-pager__btn"
        onClick={() => onChange(current - 1)}
        disabled={current <= 1}
        aria-label="Previous page"
      >
        ‹
      </button>
      {pageItems(current, total).map((item, i) =>
        item === "gap" ? (
          <span key={`gap-${i}`} className="leaderboard-pager__gap">
            …
          </span>
        ) : (
          <button
            key={item}
            className="leaderboard-pager__btn leaderboard-pager__page"
            onClick={() => onChange(item)}
            aria-current={item === current ? "page" : undefined}
          >
            {item}
          </button>
        ),
      )}
      <button
        className="leaderboard-pager__btn"
        onClick={() => onChange(current + 1)}
        disabled={current >= total}
        aria-label="Next page"
      >
        ›
      </button>
    </nav>
  );
}

export default Pager;
