import Navbar from "../components/Navbar";
import LobbyPlayerCard from "../components/LobbyPlayerCard";
import { useState, useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import "./Lobby.css";
import { useNavigate, useParams } from "react-router-dom";

function Lobby() {
  const [players, setPlayers] = useState([]);
  const { lobbyId } = useParams();
  const connectionRef = useRef(null);
  const navigate = useNavigate();

  useEffect(() => {
    const connect = async () => {
      const token = localStorage.getItem("token");
      console.log("connecting");
      const connection = new signalR.HubConnectionBuilder()
        .withUrl("/gameHub", { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .build();

      connectionRef.current = connection;

      connection.on("LobbyState", (state) => {
        setPlayers(state.players);
      });

      try {
        await connection.start();
        await connection.invoke("ConnectToLobby", lobbyId);
      } catch (e) {
        if (e.message?.includes("stopped during negotiation")) return;
        console.log(e);
        navigate("/lobbies");
        return;
      }
      console.log("connected");
    };

    connect();

    return () => {
      connectionRef.current?.stop();
    };
  }, [lobbyId, navigate]);

  const maxPerSide = 10;
  const half = Math.min(players.length, maxPerSide);
  return (
    <div className="lobby-page">
      <Navbar />

      <div className="lobby-body">
        <header className="lobby-topbar">
          <div className="lobby-topbar__info">
            <h2 className="lobby-topbar__name">shadowtyper's lobby</h2>
            <span className="lobby-topbar__count">
              {players.length}/30 players
            </span>
            <span className="lobby-topbar__mode">word count · 50 words</span>
          </div>
          <button className="lobby-topbar__leave">leave lobby</button>
        </header>

        <div className="lobby-arena">
          <div className="lobby-players-col">
            {players.slice(0, half).map((p) => (
              <LobbyPlayerCard key={p.Id} player={p} />
            ))}
          </div>

          <div className="lobby-game">
            <textarea className="lobby-game__typing-area">
              <p className="lobby-game__passage">
                the quick brown fox jumps over the lazy dog while the cat sleeps
                peacefully on the warm windowsill during a quiet afternoon
              </p>
            </textarea>
            <div className="lobby-game__waiting">
              waiting for host to start the race...
            </div>
          </div>

          <div className="lobby-players-col">
            {players.slice(half).map((p) => (
              <LobbyPlayerCard key={p.Id} player={p} />
            ))}
          </div>
        </div>

        <footer className="lobby-footer">
          <button className="lobby-footer__start">start race</button>
        </footer>
      </div>
    </div>
  );
}

export default Lobby;
