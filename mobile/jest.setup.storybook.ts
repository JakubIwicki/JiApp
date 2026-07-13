// Polyfill navigator.clipboard on globalThis.window so the instrumented
// storybook/test package (loaded transitively by @storybook/react composeStories)
// doesn't crash when its afterEach/afterAll hooks access window.navigator.clipboard.
//
// React Native's jest environment exposes globalThis.window but not navigator.
// isClipboardStub returns false for the plain object below, making the reset
// and detach hooks harmless no-ops.
const win = (globalThis as Record<string, unknown>).window as
  | Record<string, unknown>
  | undefined;

if (win && !win.navigator) {
  win.navigator = { clipboard: {} };
}
