import React from 'react';
import { render, fireEvent, act } from '@testing-library/react-native';
import SearchBar from '../SearchBar';

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        'search.placeholder': 'Search YouTube...',
      };
      return translations[key] || key;
    },
  }),
}));

describe('SearchBar', () => {
  beforeEach(() => {
    jest.useFakeTimers();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders with placeholder text from i18n', () => {
    const { getByPlaceholderText } = render(
      <SearchBar onSearch={jest.fn()} />,
    );
    expect(getByPlaceholderText('Search YouTube...')).toBeTruthy();
  });

  it('calls onSearch after 500ms debounce', () => {
    const onSearch = jest.fn();
    const { getByPlaceholderText } = render(
      <SearchBar onSearch={onSearch} />,
    );

    fireEvent.changeText(
      getByPlaceholderText('Search YouTube...'),
      'test query',
    );

    expect(onSearch).not.toHaveBeenCalled();

    act(() => {
      jest.advanceTimersByTime(500);
    });

    expect(onSearch).toHaveBeenCalledTimes(1);
    expect(onSearch).toHaveBeenCalledWith('test query');
  });

  it('does not call onSearch before debounce timeout', () => {
    const onSearch = jest.fn();
    const { getByPlaceholderText } = render(
      <SearchBar onSearch={onSearch} />,
    );

    fireEvent.changeText(
      getByPlaceholderText('Search YouTube...'),
      'test query',
    );

    act(() => {
      jest.advanceTimersByTime(300);
    });

    expect(onSearch).not.toHaveBeenCalled();
  });

  it('clear button appears when text is non-empty and clears input on press', () => {
    const onSearch = jest.fn();
    const { getByPlaceholderText, getByTestId, queryByTestId } = render(
      <SearchBar onSearch={onSearch} />,
    );

    expect(queryByTestId('search-clear-button')).toBeNull();

    fireEvent.changeText(
      getByPlaceholderText('Search YouTube...'),
      'some text',
    );

    expect(getByTestId('search-clear-button')).toBeTruthy();

    fireEvent.press(getByTestId('search-clear-button'));

    const input = getByPlaceholderText('Search YouTube...');
    expect(input.props.value).toBe('');
  });

  it('clearing input also cancels debounce and calls onSearch with empty string', () => {
    const onSearch = jest.fn();
    const { getByPlaceholderText, getByTestId } = render(
      <SearchBar onSearch={onSearch} />,
    );

    fireEvent.changeText(
      getByPlaceholderText('Search YouTube...'),
      'some text',
    );

    fireEvent.press(getByTestId('search-clear-button'));

    act(() => {
      jest.advanceTimersByTime(500);
    });

    expect(onSearch).toHaveBeenCalledWith('');
    expect(onSearch).toHaveBeenCalledTimes(1);
  });

  it('initialValue displayed on mount', () => {
    const { getByPlaceholderText } = render(
      <SearchBar onSearch={jest.fn()} initialValue="initial text" />,
    );

    const input = getByPlaceholderText('Search YouTube...');
    expect(input.props.value).toBe('initial text');
  });

  it('resets internal value when initialValue prop changes', () => {
    const { getByPlaceholderText, rerender } = render(
      <SearchBar onSearch={jest.fn()} initialValue="initial text" />,
    );

    rerender(
      <SearchBar onSearch={jest.fn()} initialValue="updated text" />,
    );

    const input = getByPlaceholderText('Search YouTube...');
    expect(input.props.value).toBe('updated text');
  });

  it('applies focused border style on focus', () => {
    const { getByPlaceholderText, getByTestId } = render(
      <SearchBar onSearch={jest.fn()} />,
    );

    const input = getByPlaceholderText('Search YouTube...');
    fireEvent(input, 'focus');

    const inputRow = getByTestId('search-input-row');
    const rowStyles = inputRow.props.style;
    const flatStyles = Array.isArray(rowStyles) ? rowStyles : [rowStyles];
    const hasPrimaryBorder = flatStyles.some(
      (s: Record<string, unknown>) => s.borderColor === '#8B7E74',
    );
    expect(hasPrimaryBorder).toBe(true);
  });

  it('resets border style on blur', () => {
    const { getByPlaceholderText, getByTestId } = render(
      <SearchBar onSearch={jest.fn()} />,
    );

    const input = getByPlaceholderText('Search YouTube...');
    fireEvent(input, 'focus');
    fireEvent(input, 'blur');

    const inputRow = getByTestId('search-input-row');
    const rowStyles = inputRow.props.style;
    const flatStyles = Array.isArray(rowStyles) ? rowStyles : [rowStyles];
    const hasDefaultBorder = flatStyles.some(
      (s: Record<string, unknown>) => s.borderColor === '#DDD6CE',
    );
    expect(hasDefaultBorder).toBe(true);
  });
});
