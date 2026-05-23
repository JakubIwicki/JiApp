import EncryptedStorage from 'react-native-encrypted-storage';
import AsyncStorage from '@react-native-async-storage/async-storage';

const TOKEN_KEY = 'auth_token';
const USER_ID_KEY = 'auth_user_id';
const DISPLAY_NAME_KEY = 'auth_display_name';
const USERNAME_KEY = 'auth_username';
const CREDENTIALS_KEY = 'saved_credentials';
const LANGUAGE_KEY = 'app_language';

interface SavedCredentials {
  username: string;
  password: string;
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
  await EncryptedStorage.setItem(
    CREDENTIALS_KEY,
    JSON.stringify(credentials),
  );
};

export const getCredentials = async (): Promise<SavedCredentials | null> => {
  const json = await EncryptedStorage.getItem(CREDENTIALS_KEY);
  if (!json) return null;
  return JSON.parse(json) as SavedCredentials;
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
