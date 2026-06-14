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
  saveCredentials,
  getCredentials,
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
const CREDENTIALS_KEY = 'saved_credentials';
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

// --- Credentials ---

describe('saveCredentials', () => {
  it('saves credentials as JSON to encrypted storage', async () => {
    const credentials = {
      username: 'johndoe',
      password: 's3cret',
      validUntil: '2026-06-01T00:00:00.000Z',
    };
    await saveCredentials(credentials);
    expect(EncryptedStorage.setItem).toHaveBeenCalledWith(
      CREDENTIALS_KEY,
      JSON.stringify(credentials),
    );
  });
});

describe('getCredentials', () => {
  it('returns parsed credentials when saved and not expired', async () => {
    const futureDate = new Date(Date.now() + 86400000).toISOString();
    const credentials = {
      username: 'johndoe',
      password: 's3cret',
      validUntil: futureDate,
    };
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(
      JSON.stringify(credentials),
    );

    const result = await getCredentials();
    expect(result).toEqual(credentials);
    expect(EncryptedStorage.getItem).toHaveBeenCalledWith(CREDENTIALS_KEY);
  });

  it('returns null when no credentials are saved', async () => {
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(null);

    const result = await getCredentials();
    expect(result).toBeNull();
  });

  it('returns null and removes expired credentials', async () => {
    const expiredDate = new Date(Date.now() - 86400000).toISOString();
    const credentials = {
      username: 'johndoe',
      password: 's3cret',
      validUntil: expiredDate,
    };
    (EncryptedStorage.getItem as jest.Mock).mockResolvedValueOnce(
      JSON.stringify(credentials),
    );

    const result = await getCredentials();
    expect(result).toBeNull();
    expect(EncryptedStorage.removeItem).toHaveBeenCalledWith(CREDENTIALS_KEY);
  });
});

describe('clearCredentials', () => {
  it('removes credentials from encrypted storage', async () => {
    await clearCredentials();
    expect(EncryptedStorage.removeItem).toHaveBeenCalledWith(CREDENTIALS_KEY);
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
