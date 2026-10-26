import type { RaceResultDTO } from "../../../../generated/signalr/FasterNFaster.Api.UseCases.Realtime.RaceFinished";

type RaceResultsProps = {
  results: RaceResultDTO[];
  selfId: string | undefined;
  onDismiss: () => void;
};

function byFinishPosition(a: RaceResultDTO, b: RaceResultDTO) {
  return (a.finishPosition ?? Infinity) - (b.finishPosition ?? Infinity);
}

function RaceResults({ results, selfId, onDismiss }: RaceResultsProps) {
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
          {[...results].sort(byFinishPosition).map((r) => (
            <tr
              key={r.playerId}
              className={r.playerId === selfId ? "current-player" : ""}
            >
              <td>{r.finishPosition ?? "-"}</td>
              <td>{r.nick}</td>
              <td>{Math.round(r.wpm)}</td>
              <td>{r.accuracy.toFixed(1)}%</td>
              <td>{r.mistakeCount}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <button className="race-results__close" onClick={onDismiss}>
        close
      </button>
    </div>
  );
}

export default RaceResults;
