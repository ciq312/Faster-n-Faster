import { NavLink } from "react-router-dom";
import "./Navbar.css";

function Navbar() {
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
      </div>
      <div className="navbar__profile">profile</div>
    </nav>
  );
}

export default Navbar;
