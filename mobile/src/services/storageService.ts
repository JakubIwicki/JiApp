import EncryptedStorage from 'react-native-encrypted-storage';
import AsyncStorage from '@react-native-async-storage/async-storage';
import type { ModuleId } from '../navigation/types';

const TOKEN_KEY = 'auth_token';
const USER_ID_KEY = 'auth_user_id';
const DISPLAY_NAME_KEY = 'auth_display_name';
const USERNAME_KEY = 'auth_username';
const CREDENTIALS_KEY = 'saved_credentials';
const LANGUAGE_KEY = 'app_language';
const SELECTED_BOARD_KEY = 'selected_board_id';
const SELECTED_MODULE_KEY = 'selected_module';
const WELCOME_SEEN_KEY = 'jiapp_welcome_seen';

interface SavedCredentials {
  username: string;
  password: string;
  validUntil: string; // ISO 8601 — after this date, credentials are discarded
}

// --- Token ---

export const saveToken = async (token: string): Promise<void> => {
  await EncryptedStorage.setItem(TOKEN_KEY, token);
};

export const getToken = async (): Promise<string | null> => {
  return EncryptedStorage.getItem(TOKEN_KEY);
};

export const clearToken = async (): Promise<void> => {
  await EncryptedStorage.removeItem(TOKEN_KEY);
};

// --- User ID ---

export const saveUserId = async (userId: number): Promise<void> => {
  await EncryptedStorage.setItem(USER_ID_KEY, String(userId));
};

export const getUserId = async (): Promise<number | null> => {
  const value = await EncryptedStorage.getItem(USER_ID_KEY);
  return value ? Number(value) : null;
};

export const clearUserId = async (): Promise<void> => {
  await EncryptedStorage.removeItem(USER_ID_KEY);
};

// --- Display Name ---

export const saveDisplayName = async (name: string): Promise<void> => {
  await EncryptedStorage.setItem(DISPLAY_NAME_KEY, name);
};

export const getDisplayName = async (): Promise<string | null> => {
  return EncryptedStorage.getItem(DISPLAY_NAME_KEY);
};

export const clearDisplayName = async (): Promise<void> => {
  await EncryptedStorage.removeItem(DISPLAY_NAME_KEY);
};

// --- Username ---

export const saveUsername = async (username: string): Promise<void> => {
  await EncryptedStorage.setItem(USERNAME_KEY, username);
};

export const getUsername = async (): Promise<string | null> => {
  return EncryptedStorage.getItem(USERNAME_KEY);
};

export const clearUsername = async (): Promise<void> => {
  await EncryptedStorage.removeItem(USERNAME_KEY);
};

// --- Credentials ---

export const saveCredentials = async (
  credentials: SavedCredentials,
): Promise<void> => {
  await EncryptedStorage.setItem(CREDENTIALS_KEY, JSON.stringify(credentials));
};

export const getCredentials = async (): Promise<SavedCredentials | null> => {
  const json = await EncryptedStorage.getItem(CREDENTIALS_KEY);
  if (!json) return null;

  const credentials = JSON.parse(json) as SavedCredentials;

  // Discard expired credentials so the user must re-login manually
  if (new Date(credentials.validUntil) < new Date()) {
    await EncryptedStorage.removeItem(CREDENTIALS_KEY);
    return null;
  }

  return credentials;
};

export const clearCredentials = async (): Promise<void> => {
  await EncryptedStorage.removeItem(CREDENTIALS_KEY);
};

// --- Language ---

export const saveLanguage = async (language: string): Promise<void> => {
  await AsyncStorage.setItem(LANGUAGE_KEY, language);
};

export const getLanguage = async (): Promise<string | null> => {
  return AsyncStorage.getItem(LANGUAGE_KEY);
};

// --- Selected Board ID ---

export const getSelectedBoardId = async (): Promise<number | null> => {
  const value = await AsyncStorage.getItem(SELECTED_BOARD_KEY);
  return value ? Number(value) : null;
};

export const saveSelectedBoardId = async (id: number): Promise<void> => {
  await AsyncStorage.setItem(SELECTED_BOARD_KEY, String(id));
};

export const clearSelectedBoardId = async (): Promise<void> => {
  await AsyncStorage.removeItem(SELECTED_BOARD_KEY);
};

// --- Selected Module ---

export const saveSelectedModule = async (moduleId: ModuleId): Promise<void> => {
  await AsyncStorage.setItem(SELECTED_MODULE_KEY, moduleId);
};

export const getSelectedModule = async (): Promise<ModuleId | null> => {
  const value = await AsyncStorage.getItem(SELECTED_MODULE_KEY);
  return value as ModuleId | null;
};

export const clearSelectedModule = async (): Promise<void> => {
  await AsyncStorage.removeItem(SELECTED_MODULE_KEY);
};

// --- Welcome Overlay ---

export const markWelcomeSeen = async (): Promise<void> => {
  await AsyncStorage.setItem(WELCOME_SEEN_KEY, '1');
};

export const hasSeenWelcome = async (): Promise<boolean> => {
  const value = await AsyncStorage.getItem(WELCOME_SEEN_KEY);
  return value === '1';
};
