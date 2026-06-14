import type { LoginResponse } from '../../types/api';

type Mode = 'success' | 'error' | 'loading';

let _mode: Mode = 'success';
let _delayMs = 0;

export const setAuthMode = (mode: Mode, delayMs = 0) => {
  _mode = mode;
  _delayMs = delayMs;
};

export const login = async (
  _username: string,
  _password: string,
): Promise<LoginResponse> => {
  if (_delayMs) await new Promise(r => setTimeout(r, _delayMs));
  if (_mode === 'loading') await new Promise(() => {});
  if (_mode === 'error') throw new Error('Invalid credentials');
  return {
    id: 1,
    displayName: 'Mock User',
    token: 'mock-jwt-token',
    modules: ['YtDownloader', 'Scheduler'],
  };
};

export const register = async (
  _username: string,
  _email: string,
  _password: string,
  _displayName: string,
): Promise<void> => {
  if (_mode === 'error') throw new Error('Registration failed');
};

export const checkToken = async (_token: string): Promise<LoginResponse> => ({
  id: 1,
  displayName: 'Mock User',
  token: _token,
  modules: ['YtDownloader', 'Scheduler'],
});
