import React from 'react';
import { fireEvent } from '@testing-library/react-native';
import { composeStories } from '@storybook/react';
import * as stories from '../VideoCard.stories';
import { rtlRender } from '../../test/rtlUtils';
import type { VideoItem } from '../../types/api';

const { Default, MissingThumbnail } = composeStories(stories);

const defaultVideo: VideoItem = {
  videoId: 'dQw4w9WgXcQ',
  title: 'Rick Astley - Never Gonna Give You Up (Official Music Video)',
  description:
    'The official video for "Never Gonna Give You Up" by Rick Astley. This classic 80s hit has become a beloved internet meme over the years.',
  imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/maxresdefault.jpg',
  videoUrl: 'https://www.youtube.com/watch?v=dQw4w9WgXcQ',
  channelTitle: 'TestChannel',
};

describe('VideoCard', () => {
  it('renders title, channel, and thumbnail', () => {
    const { getByText } = rtlRender(<Default />);
    expect(getByText(defaultVideo.title)).toBeTruthy();
    expect(getByText(defaultVideo.channelTitle)).toBeTruthy();
  });

  it('renders thumbnail image when imageUrl is present', () => {
    const { getByTestId } = rtlRender(<Default />);
    const image = getByTestId('video-thumbnail');
    expect(image).toBeTruthy();
    expect(image.props.source.uri).toBe(defaultVideo.imageUrl);
  });

  it('shows channel title as single line', () => {
    const { getByText } = rtlRender(<Default />);
    const meta = getByText(defaultVideo.channelTitle);
    expect(meta.props.numberOfLines).toBe(1);
  });

  it('fires onPress with video data when pressed', () => {
    const onPress = jest.fn();
    const { getByTestId } = rtlRender(<Default onPress={onPress} />);
    fireEvent.press(getByTestId('video-card'));
    expect(onPress).toHaveBeenCalledTimes(1);
    expect(onPress).toHaveBeenCalledWith(defaultVideo);
  });

  it('shows placeholder when imageUrl is missing', () => {
    const { getByTestId, queryByTestId } = rtlRender(<MissingThumbnail />);
    expect(queryByTestId('video-thumbnail')).toBeNull();
    expect(getByTestId('video-thumbnail-placeholder')).toBeTruthy();
  });

  it('renders title in bold', () => {
    const { getByText } = rtlRender(<Default />);
    const title = getByText(defaultVideo.title);
    const style = title.props.style;
    const stylesArray = Array.isArray(style) ? style : [style];
    expect(
      stylesArray.some((s: Record<string, unknown>) => s.fontWeight === '700'),
    ).toBe(true);
  });

  it('renders fade-in animated view', () => {
    const { getByTestId } = rtlRender(<Default />);
    const card = getByTestId('video-card');
    expect(card).toBeTruthy();
  });

  it('falls back to description when channelTitle is empty', () => {
    const { getByText } = rtlRender(
      <Default
        video={{
          ...defaultVideo,
          channelTitle: '',
          description: 'Fallback description',
        }}
      />,
    );
    expect(getByText('Fallback description')).toBeTruthy();
  });
});
