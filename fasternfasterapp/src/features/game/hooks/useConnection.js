import { useState, useEffect, useRef, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import * as signalR from "@microsoft/signalr";

export function useConnection(lobbyId) {
  const connectionRef = useRef(null);
  const selfIdRef = useRef(localStorage.getItem("userId"));
  const countdownTimersRef = useRef([]);
  const navigate = useNavigate();

  const [players, setPlayers] = useState([]);
  const [lobbyName, setLobbyName] = useState(null);
  const [lobbyMaxPlayers, setLobbyMaxPlayers] = useState(null);
  const [colors, setColors] = useState(null);
  const [raceSettings, setRaceSettings] = useState(null);

  const [isRaceStarting, setIsRaceStarting] = useState(false);
  const [isRacing, setIsRacing] = useState(false);
  const [passage, setPassage] = useState("");
  const [opponents, setOpponents] = useState([]);
  const [raceResults, setRaceResults] = useState(null);
  const [countdown, setCountdown] = useState(null);

  useEffect(() => {
    const connect = async () => {
      const token = localStorage.getItem("token");
      const connection = new signalR.HubConnectionBuilder()
        .withUrl("/gameHub", { accessTokenFactory: () => token })
        .withAutomaticReconnect()
        .build();

      connectionRef.current = connection;

      connection.on("LobbyState", (state) => {
        console.log("LobbyState:", state);
        setPlayers(state.players);
        setLobbyName(state.lobbyName);
        setLobbyMaxPlayers(state.maxPlayers);
        setColors(state.colors);
        setRaceSettings(state.raceSettings);
        setPassage(state.raceSettings.passage);
      });

      connection.on("RaceEnded", (data) => {
        setRaceResults(data.results);
        setIsRacing(false);
      });

      connection.on("RaceStarting", (data) => {
        countdownTimersRef.current.forEach(clearTimeout);
        countdownTimersRef.current = [
          setTimeout(() => setCountdown(2), 1000),
          setTimeout(() => setCountdown(1), 2000),
          setTimeout(() => setCountdown("GO"), 3000),
        ];
        setIsRaceStarting(true);
        setCountdown(3);
        setPassage(data.words);
      });

      connection.on("RaceStarted", () => {
        console.log("Race started");

        countdownTimersRef.current.forEach(clearTimeout);
        countdownTimersRef.current = [];
        setIsRaceStarting(false);
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
      }
    };

    connect();

    return () => {
      connectionRef.current?.stop();
    };
  }, [lobbyId, navigate]);

  const sendProgress = useCallback(({ index, mistakes }) => {
    console.log("Sending progress:", { index, mistakes });
    connectionRef.current?.invoke("UpdateRaceState", index, mistakes);
  }, []);

  const startRace = useCallback(async () => {
    try {
      await connectionRef.current?.invoke("StartRace");
    } catch (e) {
      console.log(e);
    }
  }, []);

  const changeColor = useCallback(async (color) => {
    try {
      await connectionRef.current?.invoke("ChangeColor", color);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const leaveLobby = useCallback(async () => {
    try {
      await connectionRef.current?.invoke("LeaveLobby");
      navigate("/lobbies");
    } catch (e) {
      console.log(e);
    }
  }, []);

  const changeGameMode = useCallback(async (mode) => {
    try {
      await connectionRef.current?.invoke("ChangeGameMode", mode);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const changeWordCount = useCallback(async (count) => {
    try {
      await connectionRef.current?.invoke("ChangeWordCount", count);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const changeTimerDuration = useCallback(async (duration) => {
    try {
      await connectionRef.current?.invoke("ChangeTimerDuration", duration);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const refreshPassage = useCallback(async () => {
    try {
      await connectionRef.current?.invoke("RefreshPassage");
    } catch (e) {
      console.log(e);
    }
  }, []);

  const dismissResults = useCallback(() => {
    setRaceResults(null);
    setPassage("");
    setOpponents([]);
  }, []);

  const isSelf = useCallback((id) => id === selfIdRef.current, []);

  return {
    selfId: selfIdRef.current,
    players,
    lobbyName,
    lobbyMaxPlayers,
    colors,
    raceSettings,
    isRaceStarting,
    isRacing,
    passage,
    opponents,
    raceResults,
    countdown,
    sendProgress,
    startRace,
    changeColor,
    changeGameMode,
    leaveLobby,
    changeWordCount,
    changeTimerDuration,
    refreshPassage,
    dismissResults,
    isSelf,
  };
}
