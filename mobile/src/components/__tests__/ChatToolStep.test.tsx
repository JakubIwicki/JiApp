import React from 'react';
import { render } from '@testing-library/react-native';
import ChatToolStep from '../chat/ChatToolStep';
import type { ToolStep } from '../../types/chat';

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const map: Record<string, string> = {
        'chat.tool.searchYoutube.running': 'Searching YouTube…',
        'chat.tool.searchYoutube.done': 'YouTube search complete',
        'chat.tool.offerDownload.running': 'Preparing download…',
        'chat.tool.offerDownload.done': 'Download ready',
      };
      return map[key] ?? key;
    },
  }),
}));

describe('ChatToolStep', () => {
  it('renders running label for search_youtube', () => {
    const step: ToolStep = {
      tool: 'search_youtube',
      status: 'running',
    };

    const { getByText } = render(<ChatToolStep step={step} />);
    expect(getByText('Searching YouTube…')).toBeTruthy();
  });

  it('renders done label with checkmark for search_youtube', () => {
    const step: ToolStep = {
      tool: 'search_youtube',
      status: 'done',
    };

    const { getByText } = render(<ChatToolStep step={step} />);
    expect(getByText('YouTube search complete')).toBeTruthy();
    expect(getByText('✓')).toBeTruthy();
  });

  it('renders running label for offer_download', () => {
    const step: ToolStep = {
      tool: 'offer_download',
      status: 'running',
    };

    const { getByText } = render(<ChatToolStep step={step} />);
    expect(getByText('Preparing download…')).toBeTruthy();
  });

  it('renders done label for offer_download', () => {
    const step: ToolStep = {
      tool: 'offer_download',
      status: 'done',
    };

    const { getByText } = render(<ChatToolStep step={step} />);
    expect(getByText('Download ready')).toBeTruthy();
    expect(getByText('✓')).toBeTruthy();
  });

  it('does not show checkmark when running', () => {
    const step: ToolStep = {
      tool: 'search_youtube',
      status: 'running',
    };

    const { queryByText } = render(<ChatToolStep step={step} />);
    expect(queryByText('✓')).toBeNull();
  });
});
