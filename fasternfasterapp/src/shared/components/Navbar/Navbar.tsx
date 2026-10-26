import { NavLink, useLocation, useNavigate } from "react-router-dom";
import { useAppSelector } from "../../../app/hooks";
import { selectLobbyId } from "../../../features/lobbies/lobbySelectors";
import { lobbyPath } from "../../utils/routes";
import "./Navbar.css";

function Navbar() {
  const lobbyId = useAppSelector(selectLobbyId);
  const navigate = useNavigate();
  const location = useLocation();
  const isOnLobbyPage = location.pathname.startsWith("/lobby/");

  return (
    <nav className="navbar">
      <NavLink to="/" className="navbar__logo">
        faster&apos;n&apos;faster
      </NavLink>
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
            onClick={() => navigate(lobbyPath(lobbyId))}
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
