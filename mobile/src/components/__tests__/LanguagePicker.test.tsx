import React from 'react';
import { render, fireEvent, act } from '@testing-library/react-native';
import i18next from '../../i18n/index';
import LanguagePicker from '../LanguagePicker';
import * as storageService from '../../services/storageService';
import { claudeLight } from '../../styles/theme';

// Mock storageService
jest.mock('../../services/storageService', () => ({
  saveLanguage: jest.fn(() => Promise.resolve()),
  getLanguage: jest.fn(() => Promise.resolve(null)),
}));

const mockSaveLanguage = storageService.saveLanguage as jest.Mock;

describe('LanguagePicker', () => {
  beforeEach(async () => {
    jest.clearAllMocks();
    await act(async () => {
      await i18next.changeLanguage('en');
    });
  });

  it('shows PL and EN toggle options', () => {
    const { getByText } = render(<LanguagePicker />);
    expect(getByText('EN')).toBeTruthy();
    expect(getByText('PL')).toBeTruthy();
  });

  it('renders pill toggle container', () => {
    const { getByTestId } = render(<LanguagePicker />);
    expect(getByTestId('language-picker-button')).toBeTruthy();
  });

  it('active pill shows white text on primary background', () => {
    const { getByText } = render(<LanguagePicker />);
    const enText = getByText('EN');
    const enStyle = enText.props.style;
    const stylesArray = Array.isArray(enStyle) ? enStyle : [enStyle];
    const hasWhiteColor = stylesArray.some(
      (s: Record<string, unknown>) => s.color === '#FFFFFF',
    );
    const hasBold = stylesArray.some(
      (s: Record<string, unknown>) => s.fontWeight === '600',
    );
    expect(hasWhiteColor).toBe(true);
    expect(hasBold).toBe(true);
  });

  it('inactive pill shows tertiary text', () => {
    const { getByText } = render(<LanguagePicker />);
    const plText = getByText('PL');
    const style = plText.props.style;
    const stylesArray = Array.isArray(style) ? style : [style];
    const hasTertiaryColor = stylesArray.some(
      (s: Record<string, unknown>) => s.color === claudeLight.textTertiary,
    );
    expect(hasTertiaryColor).toBe(true);
  });

  it('taps PL to switch to Polish', async () => {
    const { getByText } = render(<LanguagePicker />);
    fireEvent.press(getByText('PL'));
    await act(async () => {
      await new Promise(r => setTimeout(r, 0));
    });
    expect(mockSaveLanguage).toHaveBeenCalledWith('pl');
  });

  it('switches active state after toggling language', async () => {
    const { getByText } = render(<LanguagePicker />);
    fireEvent.press(getByText('PL'));
    await act(async () => {
      await new Promise(r => setTimeout(r, 0));
    });
    expect(i18next.language.startsWith('pl')).toBe(true);
    expect(mockSaveLanguage).toHaveBeenCalledWith('pl');
  });
});
