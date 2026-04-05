import { Outlet } from "react-router-dom";
import ConnectionProvider from "./ConnectionProvider";

function ConnectionLayout() {
  return (
    <ConnectionProvider url="/gameHub">
      <Outlet />
    </ConnectionProvider>
  );
}

export default ConnectionLayout;
