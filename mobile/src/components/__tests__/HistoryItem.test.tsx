import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import HistoryItem from '../HistoryItem';
import type {
  SearchHistoryItem,
  DownloadHistoryItem,
} from '../../types/api';

const createSearchItem = (
  overrides: Partial<SearchHistoryItem> = {},
): SearchHistoryItem => ({
  id: 1,
  searchText: 'test query',
  searchedAt: '2026-05-20T10:00:00Z',
  ...overrides,
});

const createDownloadItem = (
  overrides: Partial<DownloadHistoryItem> = {},
): DownloadHistoryItem => ({
  id: 1,
  videoTitle: 'Test Video',
  videoDescription: 'A test video description',
  videoId: 'abc123',
  videoUrl: 'https://youtube.com/watch?v=abc123',
  imageUrl: 'https://i.ytimg.com/vi/abc123/default.jpg',
  downloadedAt: '2026-05-20T10:00:00Z',
  ...overrides,
});

describe('HistoryItem', () => {
  it('search type renders search icon, search text, and formatted date', () => {
    const item = createSearchItem();
    const { getByText } = render(<HistoryItem type="search" item={item} />);

    expect(getByText(item.searchText)).toBeTruthy();
    expect(getByText('20.05.2026')).toBeTruthy();
    // verify search icon is present
    expect(getByText('🔍')).toBeTruthy();
  });

  it('download type renders thumbnail, video title, and formatted date', () => {
    const item = createDownloadItem();
    const { getByText, getByTestId } = render(
      <HistoryItem type="download" item={item} onPress={jest.fn()} />,
    );

    expect(getByText(item.videoTitle)).toBeTruthy();
    expect(getByText('20.05.2026')).toBeTruthy();
    expect(getByTestId('history-thumbnail')).toBeTruthy();
  });

  it('download type shows placeholder when thumbnail missing', () => {
    const item = createDownloadItem({ imageUrl: '' });
    const { queryByTestId, getByTestId } = render(
      <HistoryItem type="download" item={item} onPress={jest.fn()} />,
    );

    expect(queryByTestId('history-thumbnail')).toBeNull();
    expect(getByTestId('history-thumbnail-placeholder')).toBeTruthy();
  });

  it('fires onPress with item data when download type is pressed', () => {
    const onPress = jest.fn();
    const item = createDownloadItem();
    const { getByTestId } = render(
      <HistoryItem type="download" item={item} onPress={onPress} />,
    );

    fireEvent.press(getByTestId('history-item-download'));
    expect(onPress).toHaveBeenCalledTimes(1);
    expect(onPress).toHaveBeenCalledWith(item);
  });
});
