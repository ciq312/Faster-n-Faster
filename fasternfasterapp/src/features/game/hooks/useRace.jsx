import { useCallback, useEffect, useRef, useState } from "react";
import { useConnection } from "../../connection/ConnectionProvider";

export function useRace() {
  const { subscribe, isConnected } = useConnection();
  const [isRacing, setIsRacing] = useState(false);
  const [isRaceStarting, setIsRaceStarting] = useState(false);
  const [raceSettings, setRaceSettings] = useState(null);
  const [raceResults, setRaceResults] = useState(null);
  const [raceParticipants, setRaceParticipants] = useState([]);
  const countdownTimersRef = useRef([]);
  const [countdown, setCountdown] = useState(null);

  useEffect(() => {
    const cleanups = [
      subscribe("RaceEnded", (data) => {
        setRaceResults(data.results);
        setRaceParticipants([]);
        setIsRacing(false);
      }),

      subscribe("LobbyState", (state) => {
        setRaceSettings(state.settings);
        setIsRacing(state.isSessionActive);
      }),

      subscribe("RaceStarting", () => {
        countdownTimersRef.current.forEach(clearTimeout);
        countdownTimersRef.current = [
          setTimeout(() => setCountdown(2), 1000),
          setTimeout(() => setCountdown(1), 2000),
          setTimeout(() => setCountdown("GO"), 3000),
        ];
        setIsRaceStarting(true);
        setRaceResults(null);
        setCountdown(3);
      }),

      subscribe("RaceStarted", () => {
        countdownTimersRef.current.forEach(clearTimeout);
        countdownTimersRef.current = [];
        setIsRaceStarting(false);
        setCountdown(null);
        setIsRacing(true);
      }),

      subscribe("RaceState", (state) => {
        setRaceParticipants(state);
      }),
    ];

    return () => cleanups.map((fn) => fn());
  }, [isConnected]);

  const dismissResults = useCallback(() => {
    setRaceResults(null);
  }, []);

  return {
    dismissResults,
    isRacing,
    isRaceStarting,
    raceSettings,
    raceResults,
    raceParticipants,
    countdown,
  };
}
