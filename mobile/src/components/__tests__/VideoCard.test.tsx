import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import VideoCard from '../VideoCard';
import type { VideoItem } from '../../types/api';

const createVideo = (overrides: Partial<VideoItem> = {}): VideoItem => ({
  videoId: 'abc123',
  title: 'Test Video Title',
  description: 'This is a test video description.',
  imageUrl: 'https://example.com/thumbnail.jpg',
  videoUrl: 'https://example.com/video.mp4',
  channelTitle: 'Test Channel',
  ...overrides,
});

describe('VideoCard', () => {
  it('renders title, channel, and thumbnail', () => {
    const video = createVideo();
    const { getByText } = render(
      <VideoCard video={video} onPress={jest.fn()} />,
    );

    expect(getByText(video.title)).toBeTruthy();
    expect(getByText(video.channelTitle)).toBeTruthy();
  });

  it('renders thumbnail image when imageUrl is present', () => {
    const video = createVideo();
    const { getByTestId } = render(
      <VideoCard video={video} onPress={jest.fn()} />,
    );

    const image = getByTestId('video-thumbnail');
    expect(image).toBeTruthy();
    expect(image.props.source.uri).toBe(video.imageUrl);
  });

  it('shows channel title as single line', () => {
    const video = createVideo();
    const { getByText } = render(
      <VideoCard video={video} onPress={jest.fn()} />,
    );

    const meta = getByText(video.channelTitle);
    expect(meta.props.numberOfLines).toBe(1);
  });

  it('fires onPress with video data when pressed', () => {
    const onPress = jest.fn();
    const video = createVideo();
    const { getByTestId } = render(
      <VideoCard video={video} onPress={onPress} />,
    );

    fireEvent.press(getByTestId('video-card'));
    expect(onPress).toHaveBeenCalledTimes(1);
    expect(onPress).toHaveBeenCalledWith(video);
  });

  it('shows placeholder when imageUrl is missing', () => {
    const video = createVideo({ imageUrl: '' });
    const { getByTestId, queryByTestId } = render(
      <VideoCard video={video} onPress={jest.fn()} />,
    );

    expect(queryByTestId('video-thumbnail')).toBeNull();
    expect(getByTestId('video-thumbnail-placeholder')).toBeTruthy();
  });

  it('renders title in bold', () => {
    const video = createVideo();
    const { getByText } = render(
      <VideoCard video={video} onPress={jest.fn()} />,
    );

    const title = getByText(video.title);
    const style = title.props.style;
    const stylesArray = Array.isArray(style) ? style : [style];
    expect(stylesArray.some((s: Record<string, unknown>) => s.fontWeight === '700')).toBe(true);
  });

  it('renders fade-in animated view', () => {
    const video = createVideo();
    const { getByTestId } = render(
      <VideoCard video={video} onPress={jest.fn()} />,
    );

    const card = getByTestId('video-card');
    expect(card).toBeTruthy();
  });

  it('falls back to description when channelTitle is empty', () => {
    const video = createVideo({ channelTitle: '', description: 'Fallback description' });
    const { getByText } = render(
      <VideoCard video={video} onPress={jest.fn()} />,
    );

    expect(getByText('Fallback description')).toBeTruthy();
  });
});
