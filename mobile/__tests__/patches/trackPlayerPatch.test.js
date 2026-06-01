const fs = require('fs');
const path = require('path');

const MODULE_PATH = path.resolve(
  __dirname,
  '../../node_modules/react-native-track-player/android/src/main/java/com/doublesymmetry/trackplayer/module/MusicModule.kt',
);

const SERVICE_PATH = path.resolve(
  __dirname,
  '../../node_modules/react-native-track-player/android/src/main/java/com/doublesymmetry/trackplayer/service/MusicService.kt',
);

describe('react-native-track-player patch: MusicModule.kt', () => {
  let content;

  beforeAll(() => {
    if (!fs.existsSync(MODULE_PATH)) {
      throw new Error(`MusicModule.kt not found at ${MODULE_PATH}. Did npm install run?`);
    }
    content = fs.readFileSync(MODULE_PATH, 'utf8');
  });

  it('file exists and is a non-empty Kotlin source', () => {
    expect(content.length).toBeGreaterThan(1000);
    expect(content).toContain('class MusicModule');
    expect(content).toContain('@ReactMethod');
  });

  it('has originalItem!! null assertions in getTrack (patch hunk 1)', () => {
    // The patch adds non-null assertion (!!) on originalItem to fix
    // TypeScript/RN 0.85 null-safety interop in the getTrack method.
    expect(content).toContain(
      'callback.resolve(Arguments.fromBundle(musicService.tracks[index].originalItem!!))',
    );
  });

  it('has originalItem!! null assertions in getActiveTrack (patch hunk 2)', () => {
    // The patch adds non-null assertion (!!) on originalItem in getActiveTrack.
    expect(content).toContain(
      'musicService.tracks[musicService.getCurrentTrackIndex()].originalItem!!',
    );
  });
});

describe('react-native-track-player patch: MusicService.kt', () => {
  let content;

  beforeAll(() => {
    if (!fs.existsSync(SERVICE_PATH)) {
      throw new Error(`MusicService.kt not found at ${SERVICE_PATH}. Did npm install run?`);
    }
    content = fs.readFileSync(SERVICE_PATH, 'utf8');
  });

  it('MusicService extends HeadlessJsTaskService', () => {
    // The patch-package does not currently modify MusicService.kt.
    // The library already extends HeadlessJsTaskService which is the
    // expected base class for RN 0.85 compatibility.
    expect(content).toContain('class MusicService : HeadlessJsTaskService()');
  });
});
