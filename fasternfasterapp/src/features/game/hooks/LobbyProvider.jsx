import { createContext, useContext, useState } from "react";

const LobbyContext = createContext();

export function LobbyProvider({ children }) {
  const [lobbyId, setLobbyId] = useState(null);

  return (
    <LobbyContext.Provider value={{ lobbyId, setLobbyId }}>
      {children}
    </LobbyContext.Provider>
  );
}

export function useLobbyContext() {
  return useContext(LobbyContext);
}
