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
import { modulesFromPermissions, isAdminRole } from '../utils/permissions';

interface AuthState {
  token: string | null;
  userId: number | null;
  displayName: string | null;
  username: string | null;
  roles: string[];
  permissions: string[];
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
      roles: string[];
      permissions: string[];
    }
  | { type: 'LOGOUT' }
  | {
      type: 'RESTORE_TOKEN';
      token: string;
      userId: number;
      displayName: string;
      username: string | null;
      roles: string[];
      permissions: string[];
    }
  | { type: 'SET_LOADING'; isLoading: boolean }
  | { type: 'SHOW_WELCOME'; showWelcome: boolean }
  | { type: 'SHOW_FAREWELL'; showFarewell: boolean }
  | { type: 'UPDATE_PROFILE'; displayName: string };

interface AuthContextValue extends AuthState {
  isAdmin: boolean;
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
  updateProfile: (displayName: string, email: string) => Promise<void>;
}

const initialState: AuthState = {
  token: null,
  userId: null,
  displayName: null,
  username: null,
  roles: [],
  permissions: [],
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
        roles: action.roles,
        permissions: action.permissions,
        availableModules: modulesFromPermissions(action.permissions),
        isLoading: false,
        showWelcome: true,
      };
    case 'LOGOUT':
      return {
        token: null,
        userId: null,
        displayName: null,
        username: null,
        roles: [],
        permissions: [],
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
        roles: action.roles,
        permissions: action.permissions,
        availableModules: modulesFromPermissions(action.permissions),
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
    case 'UPDATE_PROFILE':
      return {
        ...state,
        displayName: action.displayName,
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
  roles: [],
  permissions: [],
  availableModules: [],
  isLoading: true,
  showWelcome: false,
  showFarewell: false,
  isAdmin: false,
  login: async () => {},
  register: async () => {},
  logout: async () => {},
  checkToken: async () => {},
  dismissWelcome: () => {},
  dismissFarewell: () => {},
  updateProfile: async () => {},
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

      const user = await authService.checkToken(storedToken);
      const storedUsername = await storageService.getUsername();
      dispatch({
        type: 'RESTORE_TOKEN',
        token: storedToken,
        userId: user.id,
        displayName: user.displayName,
        username: storedUsername,
        roles: user.roles,
        permissions: user.permissions,
      });
    } catch {
      await Promise.all([
        storageService.clearToken(),
        storageService.clearRefreshToken(),
        storageService.clearUserId(),
        storageService.clearDisplayName(),
        storageService.clearUsername(),
        storageService.clearCredentials(),
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
      const storageOps: Promise<void>[] = [
        storageService.saveToken(user.token),
        storageService.saveUserId(user.id),
        storageService.saveDisplayName(user.displayName),
        storageService.saveUsername(username),
      ];
      if (user.refreshToken) {
        storageOps.push(storageService.saveRefreshToken(user.refreshToken));
      }
      await Promise.all(storageOps);
      dispatch({
        type: 'LOGIN',
        token: user.token,
        userId: user.id,
        displayName: user.displayName,
        username,
        roles: user.roles,
        permissions: user.permissions,
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
      storageService.clearRefreshToken(),
      storageService.clearUserId(),
      storageService.clearDisplayName(),
      storageService.clearUsername(),
      storageService.clearCredentials(),
      storageService.clearSelectedModule(),
    ]);
    dispatch({ type: 'LOGOUT' });
  }, []);

  const updateProfile = useCallback(
    async (displayName: string, email: string) => {
      await authService.updateProfile(displayName, email);
      await storageService.saveDisplayName(displayName);
      dispatch({ type: 'UPDATE_PROFILE', displayName });
    },
    [],
  );

  const value = useMemo(
    () => ({
      token: state.token,
      userId: state.userId,
      displayName: state.displayName,
      username: state.username,
      roles: state.roles,
      permissions: state.permissions,
      availableModules: state.availableModules,
      isLoading: state.isLoading,
      showWelcome: state.showWelcome,
      showFarewell: state.showFarewell,
      isAdmin: isAdminRole(state.roles),
      login,
      register,
      logout,
      checkToken,
      dismissWelcome,
      dismissFarewell,
      updateProfile,
    }),
    [
      state,
      login,
      register,
      logout,
      checkToken,
      dismissWelcome,
      dismissFarewell,
      updateProfile,
    ],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};
