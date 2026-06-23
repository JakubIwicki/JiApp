import React from 'react';
import { render } from '@testing-library/react-native';
import ChatBubble from '../chat/ChatBubble';
import type { ChatMessage } from '../../types/chat';

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

describe('ChatBubble', () => {
  it('renders assistant message with text', () => {
    const msg: ChatMessage = {
      id: '1',
      role: 'assistant',
      text: 'Hello there',
    };

    const { getByText } = render(<ChatBubble message={msg} />);
    expect(getByText('Hello there')).toBeTruthy();
  });

  it('renders user message with text', () => {
    const msg: ChatMessage = {
      id: '2',
      role: 'user',
      text: 'Hi assistant!',
    };

    const { getByText } = render(<ChatBubble message={msg} />);
    expect(getByText('Hi assistant!')).toBeTruthy();
  });

  it('renders typing indicator when pending with no text and no tool steps', () => {
    const msg: ChatMessage = {
      id: '3',
      role: 'assistant',
      text: '',
      pending: true,
      toolSteps: [],
    };

    const { getByText } = render(<ChatBubble message={msg} />);
    // Typing dots will show at least one dot
    expect(getByText(/\./)).toBeTruthy();
  });

  it('renders streaming caret when pending with text', () => {
    const msg: ChatMessage = {
      id: '4',
      role: 'assistant',
      text: 'Partial response',
      pending: true,
    };

    const { getByText } = render(<ChatBubble message={msg} />);
    expect(getByText('Partial response')).toBeTruthy();
    expect(getByText(/▍/)).toBeTruthy();
  });

  it('renders without error for assistant message', () => {
    const msg: ChatMessage = {
      id: '5',
      role: 'assistant',
      text: 'Test',
    };

    const { getByText } = render(<ChatBubble message={msg} />);
    expect(getByText('Test')).toBeTruthy();
  });
});
