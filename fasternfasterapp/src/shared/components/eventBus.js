const listeners = {};

export const eventBus = {
  on(event, cb) {
    (listeners[event] ??= []).push(cb);
  },
  off(event, cb) {
    const cbs = listeners[event];
    if (cbs) listeners[event] = cbs.filter((fn) => fn !== cb);
  },
  emit(event, data) {
    console.log(`bus emmiting`);
    listeners[event]?.forEach((cb) => cb(data));
  },
};
