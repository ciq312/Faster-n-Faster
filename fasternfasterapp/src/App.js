import { useState } from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import "./App.css";
import { getPlayer } from "./playerIdentity";
import WelcomeModal from "./components/WelcomeModal";
import HomePage from "./pages/HomePage";
import CreateLobbyPage from "./pages/CreateLobbyPage";
import LobbyRoomPage from "./pages/LobbyRoomPage";

function App() {
  const [player, setPlayer] = useState(getPlayer);

  if (!player) {
    return <WelcomeModal onLogin={setPlayer} />;
  }

  return (
    <Routes>
      <Route path="/" element={<HomePage player={player} />} />
      <Route path="/create" element={<CreateLobbyPage player={player} />} />
      <Route path="/lobby/:lobbyId" element={<LobbyRoomPage player={player} />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default App;
