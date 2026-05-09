import { Outlet } from "react-router-dom";
import ConnectionProvider from "./ConnectionProvider";
import { LobbyProvider } from "../game/hooks/LobbyProvider";
import { API_BASE } from "../../shared/utils/apiCall";

function ConnectionLayout() {
  return (
    <ConnectionProvider url={`${API_BASE}/gameHub`}>
      <LobbyProvider>
        <Outlet />
      </LobbyProvider>
    </ConnectionProvider>
  );
}

export default ConnectionLayout;
