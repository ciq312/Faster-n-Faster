import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";
import "./App.css";
import Registration from "./pages/Registration";
import Lobbies from "./pages/Lobbies";
import Lobby from "./pages/Lobby";
import Leaderboard from "./pages/Leaderboard";

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Registration />} />
        <Route path="/lobbies" element={<Lobbies />} />
        <Route path="/lobby/:lobbyId" element={<Lobby />} />
        <Route path="/leaderboard" element={<Leaderboard />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Router>
  );
}

export default App;
