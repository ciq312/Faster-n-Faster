function RaceResults({ results, selfId, onDismiss }) {
  return (
    <div className="race-results">
      <h3 className="race-results__title">race results</h3>
      <table className="race-results__table">
        <thead>
          <tr>
            <th>#</th>
            <th>player</th>
            <th>wpm</th>
            <th>accuracy</th>
            <th>mistakes</th>
          </tr>
        </thead>
        <tbody>
          {results
            .sort((r1, r2) => {
              if (r1 === null && r2 === null) return 0;
              if (r1 === null) return 1;
              if (r2 === null) return -1;
              return r1.finishPosition - r2.finishPosition;
            })
            .map(
              (r) =>
                r && (
                  <tr
                    key={r.playerId}
                    className={r.playerId === selfId ? "current-player" : ""}
                  >
                    <td>{r.finishPosition ?? "-"}</td>
                    <td>{r.nick ?? "unknown"}</td>
                    <td>{Math.round(r.wpm)}</td>
                    <td>{(r.accuracy * 100).toFixed(1)}%</td>
                    <td>{r.mistakeCount}</td>
                  </tr>
                ),
              // !r && <tr>player left the lobby</tr>,
            )}
        </tbody>
      </table>
      <button className="race-results__close" onClick={onDismiss}>
        close
      </button>
    </div>
  );
}

export default RaceResults;
