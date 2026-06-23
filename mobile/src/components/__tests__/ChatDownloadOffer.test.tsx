import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import ChatDownloadOffer from '../chat/ChatDownloadOffer';
import type { DownloadOfferData } from '../../types/chat';

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const map: Record<string, string> = {
        'chat.offer.download': 'Download',
        'chat.offer.downloading': 'Downloading…',
        'chat.offer.done': 'Downloaded',
        'chat.offer.retry': 'Retry',
      };
      return map[key] ?? key;
    },
  }),
}));

const offer: DownloadOfferData = {
  videoId: 'vid123',
  videoUrl: 'https://youtu.be/vid123',
  title: 'A Great Song',
  imageUrl: 'https://img.example.com/thumb.jpg',
};

describe('ChatDownloadOffer', () => {
  it('renders offer with video card and download button (idle)', () => {
    const onConfirm = jest.fn();
    const { getByText } = render(
      <ChatDownloadOffer offer={offer} status="idle" onConfirm={onConfirm} />,
    );

    expect(getByText('A Great Song')).toBeTruthy();
    expect(getByText('Download')).toBeTruthy();
  });

  it('calls onConfirm when button is pressed', () => {
    const onConfirm = jest.fn();
    const { getByTestId } = render(
      <ChatDownloadOffer offer={offer} status="idle" onConfirm={onConfirm} />,
    );

    fireEvent.press(getByTestId('button'));
    expect(onConfirm).toHaveBeenCalledTimes(1);
  });

  it('shows downloading state (button disabled + spinner)', () => {
    const onConfirm = jest.fn();
    const { getByTestId } = render(
      <ChatDownloadOffer
        offer={offer}
        status="downloading"
        onConfirm={onConfirm}
      />,
    );

    // Button should show loading indicator
    expect(getByTestId('button-loading')).toBeTruthy();
  });

  it('shows done state (button disabled, no spinner)', () => {
    const onConfirm = jest.fn();
    const { getByText, queryByTestId } = render(
      <ChatDownloadOffer offer={offer} status="done" onConfirm={onConfirm} />,
    );

    expect(getByText('Downloaded')).toBeTruthy();
    expect(queryByTestId('button-loading')).toBeNull();
  });

  it('shows retry label on error', () => {
    const onConfirm = jest.fn();
    const { getByText } = render(
      <ChatDownloadOffer offer={offer} status="error" onConfirm={onConfirm} />,
    );

    expect(getByText('Retry')).toBeTruthy();
  });
});
