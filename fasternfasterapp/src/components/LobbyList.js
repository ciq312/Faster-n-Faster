import JoinButton from "./JoinButton";
import "./LobbyList.css";

function LobbyList({ lobbies, onJoin }) {
  if (!lobbies?.length) {
    return <p className="lobby-list-empty">No lobbies available.</p>;
  }

  return (
    <ul className="lobby-list">
      {lobbies.map((lobby) => (
        <li key={lobby.id} className="lobby-row">
          <span className="lobby-name">{lobby.name}</span>
          <span className="lobby-meta">
            <span className={`lobby-access ${lobby.isPrivate ? "private" : "public"}`}>
              {lobby.isPrivate ? "Private" : "Public"}
            </span>
            <span className="lobby-players">
              {lobby.playerCount}/{lobby.maxPlayers}
            </span>
            <JoinButton onJoin={() => onJoin(lobby.id)} />
          </span>
        </li>
      ))}
    </ul>
  );
}

export default LobbyList;
