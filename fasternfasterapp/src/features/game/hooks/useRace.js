import { useCallback, useEffect, useRef, useState } from "react";
import { useBannerMessage } from "../../../shared/components/BannerProvider";
import { useConnection } from "../../connection/ConnectionProvider";

export function useRace() {
  const { invoke, subscribe } = useConnection();
  const selfIfRef = useRef(localStorage.getItem("userId"));
  const [isRacing, setIsRacing] = useState(false);
  const [isRaceStarting, setIsRaceStarting] = useState(false);
  const [raceSettings, setRaceSettings] = useState(null);
  const [raceResults, setRaceResults] = useState(null);
  const [raceParticipants, setRaceParticipants] = useState([]);
  const { showMessage } = useBannerMessage();
  const countdownTimersRef = useRef([]);
  const [countdown, setCountdown] = useState(null);
  const [tier, setTier] = useState(null);

  const tiers = [
    { min: 120, label: "That's can't be real" },
    { min: 110, label: "Are you a God?" },
    { min: 100, label: "Are you even a human?" },
    { min: 90, label: "You are exceptional" },
    { min: 80, label: "You are insane" },
    { min: 70, label: "You are pretty good at it" },
    { min: 60, label: "You are not bad at all" },
    { min: 50, label: "You've used the keyboard" },
    { min: 40, label: "You are an average" },
    { min: 30, label: "You are slightly faster than a turtle" },
    { min: 20, label: "My grandma does better" },
    { min: 10, label: "So you can type, huh?" },
    { min: 0, label: "Type your first word" },
  ];

  const getTier = (value, tiers) => tiers.find((t) => value >= t.min) ?? null;

  const isSelf = (id) => id == selfIfRef.current;

  useEffect(() => {
    const cleanups = [
      subscribe("RaceEnded", (data) => {
        setRaceResults(data.results);
        setRaceParticipants([]);
        setIsRacing(false);
      }),

      subscribe("LobbyState", (state) => {
        setRaceSettings(state.settings);
      }),

      subscribe("RaceStarting", (data) => {
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
        console.log("Race started");

        countdownTimersRef.current.forEach(clearTimeout);
        countdownTimersRef.current = [];
        setIsRaceStarting(false);
        setCountdown(null);
        setIsRacing(true);
      }),

      subscribe("PlayerFinished", (data) => {
        showMessage(
          `${data.nick} finished ${data.finishPosition} with wpm:${Math.trunc(Number(data.wpm))}`,
        );
      }),
      subscribe("RaceState", (state) => {
        setRaceParticipants(state.players);
        setTier(
          getTier(state.players.find((p) => isSelf(p.playerId)).wpm, tiers),
        );
      }),
    ];

    return () => cleanups.map((fn) => fn());
  }, []);

  const sendProgress = useCallback(async ({ index, mistakes }) => {
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
    tier,
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
  };
}
