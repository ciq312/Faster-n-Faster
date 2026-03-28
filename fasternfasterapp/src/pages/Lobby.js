import Navbar from "../components/Navbar";
import LobbyPlayerCard from "../components/LobbyPlayerCard";
import TypingArea from "../components/TypingArea";
import { useState, useEffect, useRef, useCallback } from "react";
import * as signalR from "@microsoft/signalr";
import "./Lobby.css";
import { useNavigate, useParams } from "react-router-dom";

function Lobby() {
  const [players, setPlayers] = useState([]);
  const { lobbyId } = useParams();
  const connectionRef = useRef(null);

  const selfIdRef = useRef(localStorage.getItem("userId"));
  const navigate = useNavigate();

  const [lobbyName, setLobbyName] = useState(null);
  const [lobbyMaxPlayers, setLobbyMaxPlayers] = useState(null);
  const [isRacing, setIsRacing] = useState(false);
  const [passage, setPassage] = useState("");
  const [opponents, setOpponents] = useState([]);
  const [raceResults, setRaceResults] = useState(null);
  const [countdown, setCountdown] = useState(false);
  const [colors, setColors] = useState(null);

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
        console.log(state);
        setPlayers(state.players);
        setLobbyName(state.lobbyName);
        setLobbyMaxPlayers(state.maxPlayers);
        setColors(state.colors);
      });

      connection.on("RaceEnded", (data) => {
        console.log("RaceEnded", data);
        setRaceResults(data.results);
        setIsRacing(false);
      });

      connection.on("RaceStarting", (data) => {
        setCountdown(3);
        setTimeout(() => setCountdown(2), 1000);
        setTimeout(() => setCountdown(1), 2000);
        setTimeout(() => setCountdown("GO"), 3000);
        setPassage(data.words);
      });

      connection.on("RaceStarted", () => {
        console.log("RACE STARTED");
        setCountdown(null);
        setIsRacing(true);
      });

      connection.on("RaceState", (state) => {
        setOpponents(state.players);
      });

      connection.onclose((err) => console.log("Connection closed:", err));

      connection.onreconnecting((err) => console.log("Reconnecting:", err));

      connection.on("Error", (err) => console.log(err));

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

  const handleProgress = useCallback(({ index, totalTyped, mistakes }) => {
    connectionRef.current?.invoke(
      "UpdateRaceState",
      index,
      totalTyped,
      mistakes,
    );
  }, []);

  const dismissResults = () => {
    setRaceResults(null);
    setPassage("");
    setOpponents([]);
  };

  const openPalette = (id) => (id === selfIdRef.current ? true : false);

  const changeColor = useCallback(async (color) => {
    try {
      await connectionRef.current?.invoke("ChangeColor", color);
    } catch (e) {
      console.log(e);
    }
  });

  const startRace = async () => {
    try {
      console.log("Connection :", connectionRef.current?.state);
      await connectionRef.current?.invoke("StartRace");
    } catch (e) {
      console.log(e);
    }
  };

  const maxPerSide = 10;
  const half = Math.min(players.length, maxPerSide);
  return (
    <div className="lobby-page">
      <Navbar />
      <div className="lobby-body">
        <header className="lobby-topbar">
          <div className="lobby-topbar__info">
            <h2 className="lobby-topbar__name">{lobbyName}</h2>
            <span className="lobby-topbar__count">
              {players.length}/{lobbyMaxPlayers} players
            </span>
            <span className="lobby-topbar__mode">word count · 50 words</span>
          </div>
          <button className="lobby-topbar__leave">leave lobby</button>
        </header>

        <div className="lobby-arena">
          <div className="lobby-players-col">
            {players.slice(0, half).map((p) => (
              <LobbyPlayerCard
                key={p.id}
                player={p}
                colors={colors}
                openPalette={openPalette}
                changeColor={changeColor}
              />
            ))}
          </div>

          <div className="lobby-game">
            <div className="countdown-overlay">{countdown}</div>
            {raceResults ? (
              <div className="race-results">
                <h3 className="race-results__title">race results</h3>
                <table className="race-results__table">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>player</th>
                      <th>wpm</th>
                      <th>accuracy</th>
                      <th>mistakes</th>
                    </tr>
                  </thead>
                  <tbody>
                    {raceResults
                      .sort((r1, r2) => r1.finishPosition - r2.finishPosition)
                      .map((r) => {
                        return (
                          <tr
                            key={r.playerId}
                            className={
                              r.playerId === selfIdRef.current
                                ? "current-player"
                                : ""
                            }
                          >
                            <td>{r.finishPosition ?? "-"}</td>
                            <td>{r.nick ?? "unknown"}</td>
                            <td>{Math.round(r.wpm)}</td>
                            <td>{(r.accuracy * 100).toFixed(1)}%</td>
                            <td>{r.mistakeCount}</td>
                          </tr>
                        );
                      })}
                  </tbody>
                </table>
                <button
                  className="race-results__close"
                  onClick={dismissResults}
                >
                  close
                </button>
              </div>
            ) : passage ? (
              <TypingArea
                passage={passage}
                disabled={!isRacing}
                onProgress={handleProgress}
                opponents={opponents}
              />
            ) : (
              <div className="lobby-game__waiting">
                waiting for host to start the race...
              </div>
            )}
          </div>

          <div className="lobby-players-col">
            {players.slice(half).map((p) => (
              <LobbyPlayerCard key={p.id} player={p} />
            ))}
          </div>
        </div>

        <footer className="lobby-footer">
          <button
            className="lobby-footer__start"
            disabled={isRacing}
            onClick={startRace}
          >
            start race
          </button>
        </footer>
      </div>
    </div>
  );
}

export default Lobby;
