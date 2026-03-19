import "./JoinButton.css";

function JoinButton({ onJoin }) {
  return (
    <button className="join-btn" onClick={onJoin}>
      Join
    </button>
  );
}

export default JoinButton;
