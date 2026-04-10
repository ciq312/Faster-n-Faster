import { NavLink, useNavigate, useLocation } from "react-router-dom";
import { useLobbyContext } from "../../../features/game/hooks/LobbyProvider";
import "./Navbar.css";

function Navbar() {
  const { lobbyId } = useLobbyContext();
  const navigate = useNavigate();
  const location = useLocation();
  const isOnLobbyPage = location.pathname.startsWith("/lobby/");

  return (
    <nav className="navbar">
      <div className="navbar__logo">faster'n'faster</div>
      <div className="navbar__links">
        <NavLink to="/lobbies" className="navbar__link">
          lobbies
        </NavLink>
        <NavLink to="/leaderboard" className="navbar__link">
          leaderboard
        </NavLink>
        <NavLink to="/profile/me" className="navbar__link">
          profile
        </NavLink>
        {lobbyId && !isOnLobbyPage && (
          <button
            className="navbar__return-lobby"
            onClick={() => navigate(`/lobby/${lobbyId}`)}
          >
            <span className="navbar__return-lobby-dot" />
            back to lobby
          </button>
        )}
      </div>
    </nav>
  );
}

export default Navbar;
