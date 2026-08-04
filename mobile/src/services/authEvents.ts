// Fired when the refresh flow fails and auth storage is cleared, so AuthContext
// can log the user out instead of leaving a session that 401s on every request.

type AuthInvalidatedHandler = () => void;

const handlers = new Set<AuthInvalidatedHandler>();

export function onAuthInvalidated(handler: AuthInvalidatedHandler): () => void {
  handlers.add(handler);
  return () => {
    handlers.delete(handler);
  };
}

export function emitAuthInvalidated(): void {
  for (const handler of handlers) {
    handler();
  }
}
