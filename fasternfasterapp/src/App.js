import {
  BrowserRouter as Router,
  Routes,
  Route,
  Navigate,
} from "react-router-dom";
import "./App.css";
import Registration from "./pages/Registration/Registration";
import Lobbies from "./pages/Lobbies/Lobbies";
import Lobby from "./pages/Lobby/Lobby";
import Leaderboard from "./pages/Leaderboard/Leaderboard";
import ConnectionLayout from "./features/connection/ConnectionLayout";
import BannerProvider from "./shared/components/BannerProvider";

function App() {
  return (
    <Router>
      <BannerProvider>
        <Routes>
          <Route path="/" element={<Registration />} />
          <Route element={<ConnectionLayout />}>
            <Route path="/lobbies" element={<Lobbies />} />
            <Route path="/lobby/:lobbyId" element={<Lobby />} />
            <Route path="/leaderboard" element={<Leaderboard />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BannerProvider>
    </Router>
  );
}

export default App;
