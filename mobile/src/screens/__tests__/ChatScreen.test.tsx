import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import type { ChatMessage } from '../../types/chat';

const mockSend = jest.fn();
const mockConfirmDownload = jest.fn();
const mockNavigate = jest.fn();
const mockSetOptions = jest.fn();

let mockMessages: ChatMessage[] = [];
let mockIsStreaming = false;
let mockError: string | null = null;

jest.mock('../../hooks/useChat', () => ({
  __esModule: true,
  default: () => ({
    messages: mockMessages,
    isStreaming: mockIsStreaming,
    error: mockError,
    send: mockSend,
    confirmDownload: mockConfirmDownload,
  }),
}));

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      if (key === 'chat.empty.examples') {
        return ['chat.empty.example1', 'chat.empty.example2'];
      }
      return key;
    },
  }),
}));

jest.mock('@react-navigation/native', () => {
  const actual = jest.requireActual('@react-navigation/native');
  return {
    ...actual,
    useNavigation: () => ({
      navigate: mockNavigate,
      setOptions: mockSetOptions,
    }),
  };
});

jest.mock('@react-navigation/elements', () => ({
  useHeaderHeight: () => 0,
}));

import ChatScreen from '../ChatScreen';

const createMessage = (overrides: Partial<ChatMessage> = {}): ChatMessage => ({
  id: 'msg-1',
  role: 'user',
  text: 'Hello assistant!',
  ...overrides,
});

describe('ChatScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockMessages = [];
    mockIsStreaming = false;
    mockError = null;
  });

  it('renders the empty state with greeting and example chips when there are no messages', () => {
    const { getByText, getByPlaceholderText } = render(<ChatScreen />);

    expect(getByText('chat.empty.greeting')).toBeTruthy();
    expect(getByText('chat.empty.example1')).toBeTruthy();
    expect(getByText('chat.empty.example2')).toBeTruthy();
    expect(getByPlaceholderText('chat.inputPlaceholder')).toBeTruthy();
    expect(mockSetOptions).toHaveBeenCalledWith({ title: 'chat.title' });
  });

  it('renders populated messages', () => {
    mockMessages = [
      createMessage({ id: 'msg-1', role: 'user', text: 'Hello assistant!' }),
      createMessage({ id: 'msg-2', role: 'assistant', text: 'Hi there!' }),
    ];

    const { getByText, queryByText } = render(<ChatScreen />);

    expect(getByText('Hello assistant!')).toBeTruthy();
    expect(getByText('Hi there!')).toBeTruthy();
    expect(queryByText('chat.empty.greeting')).toBeNull();
  });

  it('renders the error banner when useChat reports an error', () => {
    mockError = 'Something went wrong';

    const { getByText } = render(<ChatScreen />);

    expect(getByText('Something went wrong')).toBeTruthy();
  });

  it('disables the send button while streaming', () => {
    mockIsStreaming = true;

    const { getByTestId } = render(<ChatScreen />);

    expect(
      getByTestId('chat-send-button').props.accessibilityState.disabled,
    ).toBe(true);
  });

  it('calls send with the exact input text when the send button is pressed', () => {
    const { getByTestId } = render(<ChatScreen />);

    fireEvent.changeText(getByTestId('chat-input'), '  play some lofi  ');
    fireEvent.press(getByTestId('chat-send-button'));

    expect(mockSend).toHaveBeenCalledWith('play some lofi');
    expect(mockSend).toHaveBeenCalledTimes(1);
  });

  it('does not call send when the input is empty', () => {
    const { getByTestId } = render(<ChatScreen />);

    fireEvent.press(getByTestId('chat-send-button'));

    expect(mockSend).not.toHaveBeenCalled();
  });
});
