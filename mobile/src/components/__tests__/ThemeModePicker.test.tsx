import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import i18next from '../../i18n/index';
import ThemeModePicker from '../ThemeModePicker';
import { ThemeProvider } from '../../context/ThemeContext';
import * as storageService from '../../services/storageService';

describe('ThemeModePicker', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    i18next.changeLanguage('en');
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('renders all three mode labels', () => {
    const { getByText, getByTestId } = render(
      <ThemeProvider>
        <ThemeModePicker />
      </ThemeProvider>,
    );

    expect(getByText('System')).toBeTruthy();
    expect(getByText('Light')).toBeTruthy();
    expect(getByText('Dark')).toBeTruthy();
    expect(getByTestId('theme-mode-picker-button')).toBeTruthy();
  });

  it('pressing Dark persists dark mode', async () => {
    const saveSpy = jest
      .spyOn(storageService, 'saveThemeMode')
      .mockResolvedValue(undefined);

    const { getByText } = render(
      <ThemeProvider>
        <ThemeModePicker />
      </ThemeProvider>,
    );

    fireEvent.press(getByText('Dark'));

    await waitFor(() => {
      expect(saveSpy).toHaveBeenCalledWith('dark');
    });
  });
});
