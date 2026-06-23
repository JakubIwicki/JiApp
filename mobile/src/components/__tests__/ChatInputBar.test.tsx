import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import ChatInputBar from '../chat/ChatInputBar';

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const map: Record<string, string> = {
        'chat.inputPlaceholder': 'Ask for a song…',
        'chat.send': 'Send',
      };
      return map[key] ?? key;
    },
  }),
}));

describe('ChatInputBar', () => {
  it('renders with placeholder text', () => {
    const { getByPlaceholderText } = render(
      <ChatInputBar onSend={jest.fn()} />,
    );
    expect(getByPlaceholderText('Ask for a song…')).toBeTruthy();
  });

  it('calls onSend with trimmed text and clears input', () => {
    const onSend = jest.fn();
    const { getByTestId, getByPlaceholderText } = render(
      <ChatInputBar onSend={onSend} />,
    );

    fireEvent.changeText(
      getByPlaceholderText('Ask for a song…'),
      '  Hello world  ',
    );
    fireEvent.press(getByTestId('chat-send-button'));

    expect(onSend).toHaveBeenCalledWith('Hello world');

    // Input should be cleared
    const input = getByPlaceholderText('Ask for a song…');
    expect(input.props.value).toBe('');
  });

  it('does not call onSend when input is empty', () => {
    const onSend = jest.fn();
    const { getByTestId } = render(<ChatInputBar onSend={onSend} />);

    // Send button is disabled — pressing should not trigger
    fireEvent.press(getByTestId('chat-send-button'));
    expect(onSend).not.toHaveBeenCalled();
  });

  it('does not call onSend when disabled prop is true', () => {
    const onSend = jest.fn();
    const { getByPlaceholderText, getByTestId } = render(
      <ChatInputBar onSend={onSend} disabled />,
    );

    fireEvent.changeText(getByPlaceholderText('Ask for a song…'), 'Hello');
    fireEvent.press(getByTestId('chat-send-button'));

    expect(onSend).not.toHaveBeenCalled();
  });

  it('send button is disabled (greyed) when input is empty', () => {
    const { getByTestId } = render(<ChatInputBar onSend={jest.fn()} />);

    const button = getByTestId('chat-send-button');
    expect(button.props.accessibilityState.disabled).toBe(true);
  });
});
