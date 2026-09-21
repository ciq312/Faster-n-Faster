import { useCallback, useEffect, useRef, useState } from "react";
import { useAuth } from "../../auth/AuthContext";
import { useConnection } from "../../connection/ConnectionProvider";

export function useRace() {
  const { subscribe, isConnected } = useConnection();
  const { isSelf } = useAuth();
  const [isRacing, setIsRacing] = useState(false);
  const [isRaceStarting, setIsRaceStarting] = useState(false);
  const [raceSettings, setRaceSettings] = useState(null);
  const [raceResults, setRaceResults] = useState(null);
  const [raceParticipants, setRaceParticipants] = useState([]);
  const countdownTimersRef = useRef([]);
  const [countdown, setCountdown] = useState(null);
  const [tier, setTier] = useState(null);

  const tiers = [
    { min: 120, label: "That can't be real" },
    { min: 110, label: "Are you a God?" },
    { min: 100, label: "Are you even a human?" },
    { min: 90, label: "You are exceptional" },
    { min: 80, label: "You are insane" },
    { min: 70, label: "You are pretty good at it" },
    { min: 60, label: "You are not bad at all" },
    { min: 50, label: "You've used the keyboard" },
    { min: 40, label: "You are an average" },
    { min: 30, label: "Faster than a turtle" },
    { min: 20, label: "My grandma does better" },
    { min: 10, label: "So you can type, huh?" },
    { min: 0, label: "Are you asleep?" },
  ];

  const getTier = (value, tiers) => tiers.find((t) => value >= t.min) ?? null;

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
        // setTier( 
        //   getTier(state.players.find((p) => isSelf(p.playerId)).wpm, tiers),
        // );
      }),
    ];

    return () => cleanups.map((fn) => fn());
  }, [isConnected]);

  const dismissResults = useCallback(() => {
    setRaceResults(null);
  }, []);

  return {
    tier,
    dismissResults,
    isRacing,
    isRaceStarting,
    raceSettings,
    raceResults,
    raceParticipants,
    countdown,
  };
}
