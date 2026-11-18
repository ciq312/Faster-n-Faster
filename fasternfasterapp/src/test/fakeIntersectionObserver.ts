const observers = new Set<FakeIntersectionObserver>();
let visible = false;

function entryFor(target: Element) {
  return { target, isIntersecting: true } as IntersectionObserverEntry;
}

export class FakeIntersectionObserver implements IntersectionObserver {
  readonly root = null;
  readonly rootMargin = "";
  readonly scrollMargin = "";
  readonly thresholds: readonly number[] = [];
  readonly nodes = new Set<Element>();

  constructor(private readonly callback: IntersectionObserverCallback) {
    observers.add(this);
  }

  observe(node: Element) {
    this.nodes.add(node);
    if (visible) this.callback([entryFor(node)], this);
  }

  unobserve(node: Element) {
    this.nodes.delete(node);
  }

  disconnect() {
    this.nodes.clear();
    observers.delete(this);
  }

  takeRecords(): IntersectionObserverEntry[] {
    return [];
  }

  reveal() {
    const entries = [...this.nodes].map(entryFor);
    if (entries.length > 0) this.callback(entries, this);
  }
}

export function revealObserved() {
  for (const observer of [...observers]) observer.reveal();
}

export function setObservedVisible(value: boolean) {
  visible = value;
}

export function resetObservers() {
  observers.clear();
  visible = false;
}
