import { useCallback, useRef, useState } from "react";
import { useHubEvent } from "../../connection/hooks/useHubEvent";
import { hubEvents } from "../../connection/hubEvents";

export function useRace() {
  const [isRacing, setIsRacing] = useState(false);
  const [isRaceStarting, setIsRaceStarting] = useState(false);
  const [raceSettings, setRaceSettings] = useState(null);
  const [raceResults, setRaceResults] = useState(null);
  const [raceParticipants, setRaceParticipants] = useState([]);
  const countdownTimersRef = useRef([]);
  const [countdown, setCountdown] = useState(null);
  const [isWithdrawn, setIsWithdrawn] = useState(false);

  useHubEvent(hubEvents.raceEnded, (data) => {
    setRaceResults(data.results);
    setRaceParticipants([]);
    setIsRacing(false);
  });

  useHubEvent(hubEvents.lobbyState, (state) => {
    setRaceSettings(state.settings);
    setIsRaceStarting(state.isRaceStarting);
    setIsRacing(state.isRaceStarted);
  });

  useHubEvent(hubEvents.raceStarting, ({ countdownSeconds }) => {
    countdownTimersRef.current.forEach(clearTimeout);
    countdownTimersRef.current = [];

    for (let i = countdownSeconds; i > 0; i--) {
      countdownTimersRef.current.push(
        setTimeout(() => setCountdown(i), (countdownSeconds - i) * 1000),
      );
    }

    countdownTimersRef.current.push(
      setTimeout(() => setCountdown("GO"), countdownSeconds * 1000),
    );

    setIsRaceStarting(true);
    setRaceResults(null);
  });

  useHubEvent(hubEvents.raceStarted, () => {
    countdownTimersRef.current.forEach(clearTimeout);
    countdownTimersRef.current = [];
    setIsRaceStarting(false);
    setCountdown(null);
    setIsRacing(true);
    setIsWithdrawn(false);
  });

  useHubEvent(hubEvents.raceState, (state) => {
    setRaceParticipants(state);
  });

  // Withdrawal only ends this race, not lobby membership.
  useHubEvent(hubEvents.raceWithdrawn, () => {
    setIsWithdrawn(true);
  });

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
    isWithdrawn,
  };
}
