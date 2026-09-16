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

      subscribe("RaceStarting", ({countdownSeconds}) => {
        countdownTimersRef.current.forEach(clearTimeout);
        countdownTimersRef.current = [];

        for (let i = countdownSeconds; i > 0; i--) {
          countdownTimersRef.current.push(setTimeout(() => setCountdown(i), (countdownSeconds - i) * 1000));
        }

        countdownTimersRef.current.push(setTimeout(() => setCountdown("GO"), countdownSeconds * 1000));

        setIsRaceStarting(true);
        setRaceResults(null);
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
