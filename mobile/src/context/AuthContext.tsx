import React, {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useReducer,
} from 'react';
import * as authService from '../services/authService';
import * as storageService from '../services/storageService';

interface AuthState {
  token: string | null;
  userId: number | null;
  displayName: string | null;
  username: string | null;
  isLoading: boolean;
}

type AuthAction =
  | { type: 'LOGIN'; token: string; userId: number; displayName: string; username: string }
  | { type: 'LOGOUT' }
  | { type: 'RESTORE_TOKEN'; token: string; userId: number; displayName: string; username: string | null }
  | { type: 'SET_LOADING'; isLoading: boolean };

interface AuthContextValue extends AuthState {
  login: (username: string, password: string) => Promise<void>;
  register: (
    username: string,
    email: string,
    password: string,
    displayName: string,
  ) => Promise<void>;
  logout: () => Promise<void>;
  checkToken: () => Promise<void>;
}

const initialState: AuthState = {
  token: null,
  userId: null,
  displayName: null,
  username: null,
  isLoading: true,
};

function authReducer(state: AuthState, action: AuthAction): AuthState {
  switch (action.type) {
    case 'LOGIN':
      return {
        ...state,
        token: action.token,
        userId: action.userId,
        displayName: action.displayName,
        username: action.username,
        isLoading: false,
      };
    case 'LOGOUT':
      return {
        token: null,
        userId: null,
        displayName: null,
        username: null,
        isLoading: false,
      };
    case 'RESTORE_TOKEN':
      return {
        ...state,
        token: action.token,
        userId: action.userId,
        displayName: action.displayName,
        username: action.username,
        isLoading: false,
      };
    case 'SET_LOADING':
      return {
        ...state,
        isLoading: action.isLoading,
      };
    default:
      return state;
  }
}

export const AuthContext = createContext<AuthContextValue>({
  token: null,
  userId: null,
  displayName: null,
  username: null,
  isLoading: true,
  login: async () => {},
  register: async () => {},
  logout: async () => {},
  checkToken: async () => {},
});

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [state, dispatch] = useReducer(authReducer, initialState);

  const checkToken = useCallback(async () => {
    try {
      const storedToken = await storageService.getToken();
      if (!storedToken) {
        dispatch({ type: 'LOGOUT' });
        return;
      }

      // /api/auth/me returns MeResponse { id, displayName } — no token field.
      // The existing stored token is still valid; don't overwrite it.
      const user = await authService.checkToken(storedToken);
      const storedUsername = await storageService.getUsername();
      dispatch({
        type: 'RESTORE_TOKEN',
        token: storedToken,
        userId: user.id,
        displayName: user.displayName,
        username: storedUsername,
      });
    } catch {
      // Token invalid or expired — clear everything
      await Promise.all([
        storageService.clearToken(),
        storageService.clearUserId(),
        storageService.clearDisplayName(),
        storageService.clearUsername(),
      ]);
      dispatch({ type: 'LOGOUT' });
    }
  }, []);

  useEffect(() => {
    checkToken();
  }, [checkToken]);

  const login = useCallback(
    async (username: string, password: string) => {
      dispatch({ type: 'SET_LOADING', isLoading: true });
      try {
        const user = await authService.login(username, password);
        await Promise.all([
          storageService.saveToken(user.token),
          storageService.saveUserId(user.id),
          storageService.saveDisplayName(user.displayName),
          storageService.saveUsername(username),
        ]);
        dispatch({
          type: 'LOGIN',
          token: user.token,
          userId: user.id,
          displayName: user.displayName,
          username,
        });
      } catch (error) {
        dispatch({ type: 'SET_LOADING', isLoading: false });
        throw error;
      }
    },
    [],
  );

  const register = useCallback(
    async (
      username: string,
      email: string,
      password: string,
      displayName: string,
    ) => {
      await authService.register(username, email, password, displayName);
    },
    [],
  );

  const logout = useCallback(async () => {
    await Promise.all([
      storageService.clearToken(),
      storageService.clearUserId(),
      storageService.clearDisplayName(),
      storageService.clearUsername(),
      storageService.clearCredentials(),
    ]);
    dispatch({ type: 'LOGOUT' });
  }, []);

  const value = useMemo(
    () => ({
      token: state.token,
      userId: state.userId,
      displayName: state.displayName,
      username: state.username,
      isLoading: state.isLoading,
      login,
      register,
      logout,
      checkToken,
    }),
    [state, login, register, logout, checkToken],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
