const noop = () => Promise.resolve();
export default {
  setupPlayer: noop,
  updateOptions: noop,
  reset: noop,
  add: noop,
  play: noop,
  pause: noop,
  addEventListener: () => ({ remove: noop }),
};
export const Capability = { Play: 0, Pause: 1, Stop: 2 };
export const State = { Playing: 3, Paused: 2, Stopped: 1, Ready: 0 };
export const Event = { PlaybackProgressUpdated: 'playback-progress-updated' };
export const usePlaybackState = () => ({ state: undefined });
export const useProgress = () => ({ position: 0, duration: 0 });
