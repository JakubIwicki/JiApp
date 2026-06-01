import React, { use } from 'react';
import { render, waitFor } from '@testing-library/react-native';
import { AuthProvider, AuthContext } from '../AuthContext';

// Mock authService
const mockLogin = jest.fn();
const mockRegisterFn = jest.fn();
const mockCheckToken = jest.fn();

jest.mock('../../services/authService', () => ({
  login: (...args: unknown[]) => mockLogin(...args),
  register: (...args: unknown[]) => mockRegisterFn(...args),
  checkToken: (...args: unknown[]) => mockCheckToken(...args),
}));

// Mock storageService
const mockGetToken = jest.fn();
const mockSaveToken = jest.fn();
const mockClearToken = jest.fn();
const mockSaveUserId = jest.fn();
const mockClearUserId = jest.fn();
const mockSaveDisplayName = jest.fn();
const mockClearDisplayName = jest.fn();
const mockSaveUsername = jest.fn();
const mockGetUsername = jest.fn();
const mockClearUsername = jest.fn();
const mockClearCredentials = jest.fn();

jest.mock('../../services/storageService', () => ({
  getToken: (...args: unknown[]) => mockGetToken(...args),
  saveToken: (...args: unknown[]) => mockSaveToken(...args),
  clearToken: (...args: unknown[]) => mockClearToken(...args),
  saveUserId: (...args: unknown[]) => mockSaveUserId(...args),
  clearUserId: (...args: unknown[]) => mockClearUserId(...args),
  saveDisplayName: (...args: unknown[]) => mockSaveDisplayName(...args),
  clearDisplayName: (...args: unknown[]) => mockClearDisplayName(...args),
  saveUsername: (...args: unknown[]) => mockSaveUsername(...args),
  getUsername: (...args: unknown[]) => mockGetUsername(...args),
  clearUsername: (...args: unknown[]) => mockClearUsername(...args),
  clearCredentials: (...args: unknown[]) => mockClearCredentials(...args),
  getUserId: jest.fn(() => Promise.resolve(null)),
  getDisplayName: jest.fn(() => Promise.resolve(null)),
  saveCredentials: jest.fn(),
  getCredentials: jest.fn(() => Promise.resolve(null)),
}));

describe('AuthContext', () => {
  let capturedCtx: React.ContextType<typeof AuthContext> | undefined;

  const ContextReader: React.FC = () => {
    const ctx = use(AuthContext);
    capturedCtx = ctx;
    return null;
  };

  beforeEach(() => {
    jest.clearAllMocks();
    capturedCtx = undefined;
    mockGetToken.mockResolvedValue(null);
  });

  const renderProvider = () => {
    return render(
      <AuthProvider>
        <ContextReader />
      </AuthProvider>,
    );
  };

  const waitForLoggedOut = async () => {
    await waitFor(() => {
      expect(capturedCtx).toBeDefined();
      expect(capturedCtx!.isLoading).toBe(false);
      expect(capturedCtx!.token).toBeNull();
    });
  };

  it('initial state is loading then logged out', async () => {
    renderProvider();

    await waitForLoggedOut();

    expect(capturedCtx!.userId).toBeNull();
    expect(capturedCtx!.displayName).toBeNull();
  });

  it('LOGIN sets token/userId/displayName/username', async () => {
    mockLogin.mockResolvedValueOnce({
      id: 1,
      displayName: 'Test User',
      token: 'mock-token',
    });

    renderProvider();
    await waitForLoggedOut();

    await capturedCtx!.login('testuser', 'pass123');

    await waitFor(() => {
      expect(capturedCtx!.token).toBe('mock-token');
      expect(capturedCtx!.userId).toBe(1);
      expect(capturedCtx!.displayName).toBe('Test User');
      expect(capturedCtx!.username).toBe('testuser');
    });
  });

  it('LOGOUT clears state', async () => {
    mockLogin.mockResolvedValueOnce({
      id: 1,
      displayName: 'Test User',
      token: 'mock-token',
    });

    renderProvider();
    await waitForLoggedOut();

    // Login first
    await capturedCtx!.login('testuser', 'pass123');
    await waitFor(() => {
      expect(capturedCtx!.token).toBe('mock-token');
    });

    // Then logout
    await capturedCtx!.logout();
    await waitFor(() => {
      expect(capturedCtx!.token).toBeNull();
      expect(capturedCtx!.userId).toBeNull();
      expect(capturedCtx!.displayName).toBeNull();
      expect(capturedCtx!.username).toBeNull();
    });
  });

  it('RESTORE_TOKEN hydrates state including username', async () => {
    mockGetToken.mockResolvedValue('saved-token');
    mockGetUsername.mockResolvedValue('testuser');
    mockCheckToken.mockResolvedValue({
      id: 42,
      displayName: 'Restored User',
      token: 'saved-token',
    });

    renderProvider();

    await waitFor(() => {
      expect(capturedCtx!.token).toBe('saved-token');
      expect(capturedCtx!.userId).toBe(42);
      expect(capturedCtx!.displayName).toBe('Restored User');
      expect(capturedCtx!.username).toBe('testuser');
      expect(capturedCtx!.isLoading).toBe(false);
    });
  });

  it('login() calls API and stores token', async () => {
    mockLogin.mockResolvedValueOnce({
      id: 1,
      displayName: 'Test User',
      token: 'mock-token',
    });

    renderProvider();
    await waitForLoggedOut();

    await capturedCtx!.login('testuser', 'pass123');
    await waitFor(() => {
      expect(capturedCtx!.token).toBe('mock-token');
    });

    expect(mockLogin).toHaveBeenCalledWith('testuser', 'pass123');
    expect(mockSaveToken).toHaveBeenCalledWith('mock-token');
    expect(mockSaveUserId).toHaveBeenCalledWith(1);
    expect(mockSaveDisplayName).toHaveBeenCalledWith('Test User');
  });

  it('LOGIN stores username in storage', async () => {
    mockLogin.mockResolvedValueOnce({
      id: 1,
      displayName: 'Test User',
      token: 'mock-token',
    });

    renderProvider();
    await waitForLoggedOut();

    await capturedCtx!.login('testuser', 'pass123');
    await waitFor(() => {
      expect(capturedCtx!.token).toBe('mock-token');
    });

    expect(mockSaveUsername).toHaveBeenCalledWith('testuser');
  });

  it('checkToken restores username from storage', async () => {
    mockGetToken.mockResolvedValue('stored-token');
    mockGetUsername.mockResolvedValue('storeduser');
    mockCheckToken.mockResolvedValue({
      id: 5,
      displayName: 'Stored User',
      token: 'stored-token',
    });

    renderProvider();

    await waitFor(() => {
      expect(capturedCtx!.username).toBe('storeduser');
      expect(capturedCtx!.token).toBe('stored-token');
    });

    expect(mockGetUsername).toHaveBeenCalled();
  });

  it('login() throws on bad credentials', async () => {
    mockLogin.mockRejectedValueOnce(new Error('Invalid credentials'));

    renderProvider();
    await waitForLoggedOut();

    await expect(
      capturedCtx!.login('baduser', 'wrongpass'),
    ).rejects.toThrow('Invalid credentials');
  });

  it('register() calls API (no auto-login)', async () => {
    mockRegisterFn.mockResolvedValueOnce(undefined);

    renderProvider();
    await waitForLoggedOut();

    await capturedCtx!.register(
      'newuser',
      'new@test.com',
      'pass1234',
      'New User',
    );

    expect(mockRegisterFn).toHaveBeenCalledWith(
      'newuser',
      'new@test.com',
      'pass1234',
      'New User',
    );
    // Token should still be null (no auto-login)
    expect(capturedCtx!.token).toBeNull();
  });

  it('checkToken() restores session', async () => {
    mockGetToken.mockResolvedValue('valid-token');
    mockCheckToken.mockResolvedValue({
      id: 7,
      displayName: 'Session User',
      token: 'valid-token',
    });

    renderProvider();

    await waitFor(() => {
      expect(capturedCtx!.token).toBe('valid-token');
      expect(capturedCtx!.userId).toBe(7);
      expect(capturedCtx!.displayName).toBe('Session User');
    });
  });

  it('checkToken() logs out on 401', async () => {
    mockGetToken.mockResolvedValue('expired-token');
    mockCheckToken.mockRejectedValue(new Error('Token expired'));

    renderProvider();

    await waitFor(() => {
      expect(capturedCtx!.token).toBeNull();
      expect(capturedCtx!.userId).toBeNull();
      expect(capturedCtx!.displayName).toBeNull();
      expect(capturedCtx!.isLoading).toBe(false);
    });

    expect(mockClearToken).toHaveBeenCalled();
    expect(mockClearUserId).toHaveBeenCalled();
    expect(mockClearDisplayName).toHaveBeenCalled();
  });

  it('logout() clears storage', async () => {
    renderProvider();
    await waitForLoggedOut();

    await capturedCtx!.logout();

    expect(mockClearToken).toHaveBeenCalled();
    expect(mockClearUserId).toHaveBeenCalled();
    expect(mockClearDisplayName).toHaveBeenCalled();
    expect(mockClearUsername).toHaveBeenCalled();
    expect(mockClearCredentials).toHaveBeenCalled();
  });
});
