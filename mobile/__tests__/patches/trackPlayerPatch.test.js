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

  it('has no expression-body @ReactMethod methods (no "= scope.launch {" pattern)', () => {
    // RN 0.85 TurboModule interop rejects @ReactMethod methods with non-void return types.
    // Expression-body syntax (fun X() = scope.launch { }) infers return type Job.
    // The patch converts them to block bodies so return type is Unit (void).
    const lines = content.split('\n');
    const badLines = lines.filter((line) => /=\s*scope\.launch\s*\{/.test(line));
    expect(badLines).toEqual([]);
  });

  it('has no multi-line expression-body pattern', () => {
    const lines = content.split('\n');
    const badPairs = [];
    for (let i = 0; i < lines.length - 1; i++) {
      if (
        /\)\s*=\s*$/.test(lines[i].trimEnd()) &&
        /\bscope\.launch\s*\{/.test(lines[i + 1])
      ) {
        badPairs.push(i + 1);
      }
    }
    expect(badPairs).toEqual([]);
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

  it('emit() and emitList() use reactContext, not reactNativeHost', () => {
    // RN 0.85 New Architecture: ReactApplication.getReactNativeHost() throws.
    // HeadlessJsTaskService provides 'reactContext' which handles both architectures.
    // The patch replaces reactNativeHost.reactInstanceManager.currentReactContext
    // with reactContext in emit() and emitList().
    expect(content).not.toMatch(/reactNativeHost\.reactInstanceManager\.currentReactContext/);
  });

  it('MusicService extends HeadlessJsTaskService', () => {
    expect(content).toContain('class MusicService : HeadlessJsTaskService()');
  });
});
