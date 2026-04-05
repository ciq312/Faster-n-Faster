import { useState, useEffect, useRef, useCallback, useContext } from "react";
import { useNavigate } from "react-router-dom";
import * as signalR from "@microsoft/signalr";
import { useConnection } from "../../connection/ConnectionProvider";

export function useLobbyConnection(lobbyId) {
  const { invoke, subscribe } = useConnection();
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

      subscribe("LobbyState", (state) => {
        console.log("LobbyState:", state);
        setPlayers(state.players);
        setLobbyName(state.lobbyName);
        setLobbyMaxPlayers(state.maxPlayers);
        setColors(state.colors);
        setRaceSettings(state.raceSettings);
        setPassage(state.raceSettings.passage);
      });

      subscribe("RaceEnded", (data) => {
        setRaceResults(data.results);
        setIsRacing(false);
      });

      subscribe("RaceStarting", (data) => {
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

      subscribe("RaceStarted", () => {
        console.log("Race started");

        countdownTimersRef.current.forEach(clearTimeout);
        countdownTimersRef.current = [];
        setIsRaceStarting(false);
        setCountdown(null);
        setIsRacing(true);
      });

      subscribe("RaceState", (state) => {
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
    invoke("UpdateRaceState", index, mistakes);
  }, []);

  const startRace = useCallback(async () => {
    try {
      await invoke("StartRace");
    } catch (e) {
      console.log(e);
    }
  }, []);

  const changeColor = useCallback(async (color) => {
    try {
      await invoke("ChangeColor", color);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const leaveLobby = useCallback(async () => {
    try {
      await invoke("LeaveLobby");
      navigate("/lobbies");
    } catch (e) {
      console.log(e);
    }
  }, []);

  const changeGameMode = useCallback(async (mode) => {
    try {
      await invoke("ChangeGameMode", mode);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const changeWordCount = useCallback(async (count) => {
    try {
      await invoke("ChangeWordCount", count);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const changeTimerDuration = useCallback(async (duration) => {
    try {
      await invoke("ChangeTimerDuration", duration);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const refreshPassage = useCallback(async () => {
    try {
      await invoke("RefreshPassage");
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
