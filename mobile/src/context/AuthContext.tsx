import React, {
  createContext,
  useCallback,
  useEffect,
  useMemo,
  useReducer,
} from 'react';
import * as authService from '../services/authService';
import * as storageService from '../services/storageService';
import type { ModuleId } from '../navigation/types';

interface AuthState {
  token: string | null;
  userId: number | null;
  displayName: string | null;
  username: string | null;
  availableModules: ModuleId[];
  isLoading: boolean;
  showWelcome: boolean;
  showFarewell: boolean;
}

type AuthAction =
  | {
      type: 'LOGIN';
      token: string;
      userId: number;
      displayName: string;
      username: string;
      availableModules: ModuleId[];
    }
  | { type: 'LOGOUT' }
  | {
      type: 'RESTORE_TOKEN';
      token: string;
      userId: number;
      displayName: string;
      username: string | null;
      availableModules: ModuleId[];
    }
  | { type: 'SET_LOADING'; isLoading: boolean }
  | { type: 'SHOW_WELCOME'; showWelcome: boolean }
  | { type: 'SHOW_FAREWELL'; showFarewell: boolean };

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
  dismissWelcome: () => void;
  dismissFarewell: () => void;
}

const initialState: AuthState = {
  token: null,
  userId: null,
  displayName: null,
  username: null,
  availableModules: [],
  isLoading: true,
  showWelcome: false,
  showFarewell: false,
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
        availableModules: action.availableModules,
        isLoading: false,
        showWelcome: true,
      };
    case 'LOGOUT':
      return {
        token: null,
        userId: null,
        displayName: null,
        username: null,
        availableModules: [],
        isLoading: false,
        showWelcome: false,
        showFarewell: false,
      };
    case 'RESTORE_TOKEN':
      return {
        ...state,
        token: action.token,
        userId: action.userId,
        displayName: action.displayName,
        username: action.username,
        availableModules: action.availableModules,
        isLoading: false,
        showWelcome: true,
      };
    case 'SET_LOADING':
      return {
        ...state,
        isLoading: action.isLoading,
      };
    case 'SHOW_WELCOME':
      return {
        ...state,
        showWelcome: action.showWelcome,
      };
    case 'SHOW_FAREWELL':
      return {
        ...state,
        showFarewell: action.showFarewell,
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
  availableModules: [],
  isLoading: true,
  showWelcome: false,
  showFarewell: false,
  login: async () => {},
  register: async () => {},
  logout: async () => {},
  checkToken: async () => {},
  dismissWelcome: () => {},
  dismissFarewell: () => {},
});

const KNOWN_MODULES: readonly ModuleId[] = ['YtDownloader', 'Scheduler'];

/** Keep only canonical module ids the app knows how to navigate to. */
function normalizeModules(modules: string[] | undefined): ModuleId[] {
  return (modules ?? []).filter((m): m is ModuleId =>
    KNOWN_MODULES.includes(m as ModuleId),
  );
}

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

      const user = await authService.checkToken(storedToken);
      const storedUsername = await storageService.getUsername();
      dispatch({
        type: 'RESTORE_TOKEN',
        token: storedToken,
        userId: user.id,
        displayName: user.displayName,
        username: storedUsername,
        availableModules: normalizeModules(user.modules),
      });
    } catch {
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

  const login = useCallback(async (username: string, password: string) => {
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
        availableModules: normalizeModules(user.modules),
      });
    } catch (error) {
      dispatch({ type: 'SET_LOADING', isLoading: false });
      throw error;
    }
  }, []);

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
    dispatch({ type: 'SHOW_FAREWELL', showFarewell: true });
  }, []);

  const dismissWelcome = useCallback(() => {
    dispatch({ type: 'SHOW_WELCOME', showWelcome: false });
  }, []);

  const dismissFarewell = useCallback(async () => {
    await Promise.all([
      storageService.clearToken(),
      storageService.clearUserId(),
      storageService.clearDisplayName(),
      storageService.clearUsername(),
      storageService.clearCredentials(),
      storageService.clearSelectedModule(),
    ]);
    dispatch({ type: 'LOGOUT' });
  }, []);

  const value = useMemo(
    () => ({
      token: state.token,
      userId: state.userId,
      displayName: state.displayName,
      username: state.username,
      availableModules: state.availableModules,
      isLoading: state.isLoading,
      showWelcome: state.showWelcome,
      showFarewell: state.showFarewell,
      login,
      register,
      logout,
      checkToken,
      dismissWelcome,
      dismissFarewell,
    }),
    [
      state,
      login,
      register,
      logout,
      checkToken,
      dismissWelcome,
      dismissFarewell,
    ],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
