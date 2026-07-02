import EncryptedStorage from 'react-native-encrypted-storage';
import AsyncStorage from '@react-native-async-storage/async-storage';
import {
  saveToken,
  getToken,
  clearToken,
  saveUserId,
  getUserId,
  clearUserId,
  saveDisplayName,
  getDisplayName,
  clearDisplayName,
  saveUsername,
  getUsername,
  clearUsername,
  saveRefreshToken,
  getRefreshToken,
  clearRefreshToken,
  clearCredentials,
  saveLanguage,
  getLanguage,
  saveSelectedModule,
  getSelectedModule,
  clearSelectedModule,
} from '../storageService';

const TOKEN_KEY = 'auth_token';
const USER_ID_KEY = 'auth_user_id';
const DISPLAY_NAME_KEY = 'auth_display_name';
const USERNAME_KEY = 'auth_username';
const REFRESH_TOKEN_KEY = 'auth_refresh_token';
const LEGACY_CREDENTIALS_KEY = 'saved_credentials';
const LANGUAGE_KEY = 'app_language';
const SELECTED_MODULE_KEY = 'selected_module';

beforeEach(() => {
  jest.clearAllMocks();
});

// --- Token ---

describe('saveToken', () => {
  it('saves token to encrypted storage', async () => {
    await saveToken('test-jwt-token');
    expect(EncryptedStorage.setItem).toHaveBeenCalledWith(
      TOKEN_KEY,
      'test-jwt-token',
    );
  });
});

describe('getToken', () => {
  it('returns the token when saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(
      'saved-jwt-token',
    );

    const result = await getToken();
    expect(result).toBe('saved-jwt-token');
    expect(EncryptedStorage.getItem).toHaveBeenCalledWith(TOKEN_KEY);
  });

  it('returns null when no token is saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(null);

    const result = await getToken();
    expect(result).toBeNull();
  });
});

describe('clearToken', () => {
  it('removes token from encrypted storage', async () => {
    await clearToken();
    expect(EncryptedStorage.removeItem).toHaveBeenCalledWith(TOKEN_KEY);
  });
});

// --- User ID ---

describe('saveUserId', () => {
  it('saves user id to encrypted storage as string', async () => {
    await saveUserId(42);
    expect(EncryptedStorage.setItem).toHaveBeenCalledWith(USER_ID_KEY, '42');
  });
});

describe('getUserId', () => {
  it('returns the user id as a number when saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce('99');

    const result = await getUserId();
    expect(result).toBe(99);
    expect(EncryptedStorage.getItem).toHaveBeenCalledWith(USER_ID_KEY);
  });

  it('returns null when no user id is saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(null);

    const result = await getUserId();
    expect(result).toBeNull();
  });
});

describe('clearUserId', () => {
  it('removes user id from encrypted storage', async () => {
    await clearUserId();
    expect(EncryptedStorage.removeItem).toHaveBeenCalledWith(USER_ID_KEY);
  });
});

// --- Display Name ---

describe('saveDisplayName', () => {
  it('saves display name to encrypted storage', async () => {
    await saveDisplayName('John Doe');
    expect(EncryptedStorage.setItem).toHaveBeenCalledWith(
      DISPLAY_NAME_KEY,
      'John Doe',
    );
  });
});

describe('getDisplayName', () => {
  it('returns the display name when saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce('Jane');

    const result = await getDisplayName();
    expect(result).toBe('Jane');
    expect(EncryptedStorage.getItem).toHaveBeenCalledWith(DISPLAY_NAME_KEY);
  });

  it('returns null when no display name is saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(null);

    const result = await getDisplayName();
    expect(result).toBeNull();
  });
});

describe('clearDisplayName', () => {
  it('removes display name from encrypted storage', async () => {
    await clearDisplayName();
    expect(EncryptedStorage.removeItem).toHaveBeenCalledWith(DISPLAY_NAME_KEY);
  });
});

// --- Username ---

describe('saveUsername', () => {
  it('saves username to encrypted storage', async () => {
    await saveUsername('johndoe');
    expect(EncryptedStorage.setItem).toHaveBeenCalledWith(
      USERNAME_KEY,
      'johndoe',
    );
  });
});

describe('getUsername', () => {
  it('returns the username when saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce('janedoe');

    const result = await getUsername();
    expect(result).toBe('janedoe');
    expect(EncryptedStorage.getItem).toHaveBeenCalledWith(USERNAME_KEY);
  });

  it('returns null when no username is saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(null);

    const result = await getUsername();
    expect(result).toBeNull();
  });
});

describe('clearUsername', () => {
  it('removes username from encrypted storage', async () => {
    await clearUsername();
    expect(EncryptedStorage.removeItem).toHaveBeenCalledWith(USERNAME_KEY);
  });
});

// --- Refresh Token ---

describe('saveRefreshToken', () => {
  it('saves refresh token to encrypted storage', async () => {
    await saveRefreshToken('refresh-jwt-abc');
    expect(EncryptedStorage.setItem).toHaveBeenCalledWith(
      REFRESH_TOKEN_KEY,
      'refresh-jwt-abc',
    );
  });
});

describe('getRefreshToken', () => {
  it('returns the refresh token when saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(
      'stored-refresh',
    );

    const result = await getRefreshToken();
    expect(result).toBe('stored-refresh');
    expect(EncryptedStorage.getItem).toHaveBeenCalledWith(REFRESH_TOKEN_KEY);
  });

  it('returns null when no refresh token is saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(null);

    const result = await getRefreshToken();
    expect(result).toBeNull();
  });
});

describe('clearRefreshToken', () => {
  it('removes refresh token from encrypted storage', async () => {
    await clearRefreshToken();
    expect(EncryptedStorage.removeItem).toHaveBeenCalledWith(REFRESH_TOKEN_KEY);
  });
});

// --- Legacy Credential Cleanup ---

describe('clearCredentials', () => {
  it('removes the legacy saved_credentials key (migration cleanup)', async () => {
    await clearCredentials();
    expect(EncryptedStorage.removeItem).toHaveBeenCalledWith(
      LEGACY_CREDENTIALS_KEY,
    );
  });
});

// --- Language ---

describe('saveLanguage', () => {
  it('saves language to async storage', async () => {
    await saveLanguage('pl');
    expect(AsyncStorage.setItem).toHaveBeenCalledWith(LANGUAGE_KEY, 'pl');
  });
});

describe('getLanguage', () => {
  it('returns the language when saved', async () => {
    (AsyncStorage.getItem as jest.Mock).mockResolvedValueOnce('pl');

    const result = await getLanguage();
    expect(result).toBe('pl');
    expect(AsyncStorage.getItem).toHaveBeenCalledWith(LANGUAGE_KEY);
  });

  it('returns null when no language is saved', async () => {
    (AsyncStorage.getItem as jest.Mock).mockResolvedValueOnce(null);

    const result = await getLanguage();
    expect(result).toBeNull();
  });
});

// --- Selected Module ---

describe('saveSelectedModule', () => {
  it('saves the selected module to async storage', async () => {
    await saveSelectedModule('Scheduler');
    expect(AsyncStorage.setItem).toHaveBeenCalledWith(
      SELECTED_MODULE_KEY,
      'Scheduler',
    );
  });
});

describe('getSelectedModule', () => {
  it('returns the selected module when saved', async () => {
    (AsyncStorage.getItem as jest.Mock).mockResolvedValueOnce('YtDownloader');

    const result = await getSelectedModule();
    expect(result).toBe('YtDownloader');
    expect(AsyncStorage.getItem).toHaveBeenCalledWith(SELECTED_MODULE_KEY);
  });

  it('returns null when no module is saved', async () => {
    (AsyncStorage.getItem as jest.Mock).mockResolvedValueOnce(null);

    const result = await getSelectedModule();
    expect(result).toBeNull();
  });
});

describe('clearSelectedModule', () => {
  it('removes the selected module from async storage', async () => {
    await clearSelectedModule();
    expect(AsyncStorage.removeItem).toHaveBeenCalledWith(SELECTED_MODULE_KEY);
  });
});
