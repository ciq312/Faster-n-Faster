import { useEffect, useEffectEvent, useState } from "react";

export function useOnVisible(
  onVisible: () => void,
): (node: Element | null) => void {
  const [node, setNode] = useState<Element | null>(null);
  const notify = useEffectEvent(onVisible);

  useEffect(() => {
    if (!node) return;
    const observer = new IntersectionObserver((entries) => {
      if (entries.some((entry) => entry.isIntersecting)) notify();
    });
    observer.observe(node);
    return () => observer.disconnect();
  }, [node]);

  return setNode;
}
