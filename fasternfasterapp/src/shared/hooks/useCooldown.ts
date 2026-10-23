import { useCallback, useEffect, useState } from "react";

export function useCooldown(
  seconds: number,
  { startActive = false }: { startActive?: boolean } = {},
) {
  const [cooldown, setCooldown] = useState(startActive ? seconds : 0);

  useEffect(() => {
    if (cooldown <= 0) return;
    const id = setTimeout(() => setCooldown((c) => c - 1), 1000);
    return () => clearTimeout(id);
  }, [cooldown]);

  const start = useCallback(() => setCooldown(seconds), [seconds]);

  return { cooldown, start };
}
