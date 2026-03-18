import { useState } from "react";
import "./App.css";
import LobbyList from "./LobbyList";
import CreateLobbyModal from "./CreateLobbyModal";

const mockLobbies = [
  { id: 1, name: "Speed Demons", isPrivate: false, playerCount: 3, maxPlayers: 30 },
  { id: 2, name: "Secret Race", isPrivate: true, playerCount: 1, maxPlayers: 30 },
  { id: 3, name: "Beginner Friendly", isPrivate: false, playerCount: 8, maxPlayers: 30 },
];

function App() {
  const [showCreateModal, setShowCreateModal] = useState(false);

  return (
    <div className="app">
      <div className="app-header">
        <h1 className="app-title">Faster'n'Faster</h1>
        <button className="create-btn" onClick={() => setShowCreateModal(true)}>
          + Create Lobby
        </button>
      </div>

      <LobbyList lobbies={mockLobbies} />

      {showCreateModal && (
        <CreateLobbyModal onClose={() => setShowCreateModal(false)} />
      )}
    </div>
  );
}

export default App;
