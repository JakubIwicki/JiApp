// Manual mock for react-native-sse (native ESM, untransformable by Jest).
// Provides a no-op EventSource so navigation/component render tests that
// transitively import chatService → react-native-sse don't SyntaxError.

class MockEventSource {
  addEventListener(_type: string, _listener: (event: unknown) => void): void {}
  removeEventListener(
    _type: string,
    _listener: (event: unknown) => void,
  ): void {}
  removeAllEventListeners(_type?: string): void {}
  close(): void {}
}

export default MockEventSource;
