const listeners = {};

export const eventBus = {
  on(event, cb) {
    (listeners[event] ??= []).push(cb);
  },
  emit(event, data) {
    console.log(`bus emmiting`);
    listeners[event]?.map((cb) => cb(data));
  },
};
