import { useEffect, useState, useRef, useCallback } from "react";
import { useConnection } from "../../connection/ConnectionProvider";

export function useRace() {
  const { invoke, subscribe } = useConnection();
  const [isRacing, setIsRacing] = useState(false);
  const [isRaceStarting, setIsRaceStarting] = useState(false);
  const [raceSettings, setRaceSettings] = useState(null);
  const [raceResults, setRaceResults] = useState(null);
  const [raceParticipants, setRaceParticipants] = useState([]);
  const countdownTimersRef = useRef([]);
  const [countdown, setCountdown] = useState(null);
  const [passage, setPassage] = useState(null);

  useEffect(() => {
    const cleanups = [
      subscribe("RaceEnded", (data) => {
        setRaceResults(data.results);
        setIsRacing(false);
      }),

      subscribe("LobbyState", (state) => {
        setRaceSettings(state.raceSettings);
      }),

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
      }),

      subscribe("RaceStarted", () => {
        console.log("Race started");

        countdownTimersRef.current.forEach(clearTimeout);
        countdownTimersRef.current = [];
        setIsRaceStarting(false);
        setCountdown(null);
        setIsRacing(true);
      }),

      subscribe("RaceState", (state) => {
        setRaceParticipants(state.players);
      }),
    ];

    return () => cleanups.map((fn) => fn());
  }, []);

  const sendProgress = useCallback(async ({ index, mistakes }) => {
    console.log("Sending progress:", { index, mistakes });
    try {
      await invoke("UpdateRaceState", index, mistakes);
    } catch (e) {
      console.log(e);
    }
  }, []);

  const startRace = useCallback(async () => {
    try {
      await invoke("StartRace");
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
  }, []);

  return {
    startRace,
    sendProgress,
    refreshPassage,
    changeGameMode,
    changeTimerDuration,
    changeWordCount,
    dismissResults,
    isRacing,
    isRaceStarting,
    raceSettings,
    raceResults,
    raceParticipants,
    countdown,
    passage,
  };
}
