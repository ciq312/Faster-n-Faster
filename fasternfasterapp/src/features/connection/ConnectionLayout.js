import { Outlet } from "react-router-dom";
import ConnectionProvider from "./ConnectionProvider";
import { LobbyProvider } from "../game/hooks/LobbyProvider";

function ConnectionLayout() {
  return (
    <ConnectionProvider url="/gameHub">
      <LobbyProvider>
        <Outlet />
      </LobbyProvider>
    </ConnectionProvider>
  );
}

export default ConnectionLayout;
