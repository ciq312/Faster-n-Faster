import { useCooldown } from "../../../shared/hooks/useCooldown";

function Countdown({ seconds }: { seconds: number }) {
  const { cooldown } = useCooldown(seconds, { startActive: true });
  return cooldown > 0 ? cooldown : "GO";
}

export default Countdown;
