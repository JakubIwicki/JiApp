import React from 'react';
import { Alert } from 'react-native';
import { render, fireEvent, waitFor } from '@testing-library/react-native';
import type { Board } from '../../types/api';
import BoardManagementScreen from '../BoardManagementScreen';

// Auto-confirm destructive Alert dialogs by invoking the destructive button
jest.spyOn(Alert, 'alert').mockImplementation((_title, _msg, buttons) => {
  const destructive = buttons?.find(b => b.style === 'destructive');
  destructive?.onPress?.();
});

jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) =>
      opts && 'count' in opts ? `${key}:${opts.count}` : key,
  }),
}));

const mockCreateBoard = jest.fn();
const mockDeleteBoard = jest.fn();
const mockAddMember = jest.fn();
const mockRemoveMember = jest.fn();

let mockBoardState: {
  boards: Board[];
  isLoading: boolean;
  error: string | null;
};

jest.mock('../../hooks/useBoard', () => ({
  useBoard: () => ({
    boards: mockBoardState.boards,
    selectedBoardId: mockBoardState.boards[0]?.id ?? null,
    isLoading: mockBoardState.isLoading,
    error: mockBoardState.error,
    switchBoard: jest.fn(),
    loadBoards: jest.fn(),
    createBoard: (...args: unknown[]) => mockCreateBoard(...args),
    deleteBoard: (...args: unknown[]) => mockDeleteBoard(...args),
    addMember: (...args: unknown[]) => mockAddMember(...args),
    removeMember: (...args: unknown[]) => mockRemoveMember(...args),
  }),
}));

const mockShowError = jest.fn();
const mockShowSuccess = jest.fn();
jest.mock('../../../../hooks/useToast', () => ({
  __esModule: true,
  default: () => ({
    showError: (...args: unknown[]) => mockShowError(...args),
    showSuccess: (...args: unknown[]) => mockShowSuccess(...args),
    showInfo: jest.fn(),
    showWarning: jest.fn(),
  }),
}));

const board = (id: number, name: string, members: number[] = []): Board => ({
  id,
  name,
  memberUserIds: members,
  createdAt: '2026-01-01T00:00:00.000Z',
});

describe('BoardManagementScreen', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockBoardState = {
      boards: [board(1, 'Salon A', [10, 20]), board(2, 'Salon B')],
      isLoading: false,
      error: null,
    };
  });

  it('lists every board returned by the board service', () => {
    const { getByText } = render(<BoardManagementScreen />);

    expect(getByText('Salon A')).toBeTruthy();
    expect(getByText('Salon B')).toBeTruthy();
  });

  it('shows the empty state when there are no boards', () => {
    mockBoardState.boards = [];

    const { getByText } = render(<BoardManagementScreen />);

    expect(getByText('boardManagement.empty')).toBeTruthy();
  });

  it('shows a loading indicator while boards are loading', () => {
    mockBoardState.isLoading = true;
    mockBoardState.boards = [];

    const { getByTestId } = render(<BoardManagementScreen />);

    expect(getByTestId('board-management-loading')).toBeTruthy();
  });

  it('creates a board from the create form', async () => {
    mockCreateBoard.mockResolvedValueOnce(board(3, 'Salon C'));
    const { getByTestId } = render(<BoardManagementScreen />);

    expect(mockCreateBoard).not.toHaveBeenCalled();

    fireEvent.changeText(getByTestId('board-name-input'), 'Salon C');
    fireEvent.press(getByTestId('board-create-button'));

    await waitFor(() => {
      expect(mockCreateBoard).toHaveBeenCalledWith('Salon C');
    });
  });

  it('does not create a board when the name is blank', () => {
    const { getByTestId } = render(<BoardManagementScreen />);

    fireEvent.press(getByTestId('board-create-button'));

    expect(mockCreateBoard).not.toHaveBeenCalled();
  });

  it('deletes a board when its delete control is pressed', () => {
    const { getByTestId } = render(<BoardManagementScreen />);

    fireEvent.press(getByTestId('board-delete-1'));

    expect(mockDeleteBoard).toHaveBeenCalledWith(1);
  });

  it('adds a member to the selected board', async () => {
    mockAddMember.mockResolvedValueOnce(undefined);
    const { getByTestId } = render(<BoardManagementScreen />);

    fireEvent.press(getByTestId('board-expand-1'));
    fireEvent.changeText(getByTestId('member-id-input-1'), '42');
    fireEvent.press(getByTestId('member-add-button-1'));

    await waitFor(() => {
      expect(mockAddMember).toHaveBeenCalledWith(1, 42);
    });
  });

  it('removes a member from the board', () => {
    const { getByTestId } = render(<BoardManagementScreen />);

    fireEvent.press(getByTestId('board-expand-1'));
    fireEvent.press(getByTestId('member-remove-1-10'));

    expect(mockRemoveMember).toHaveBeenCalledWith(1, 10);
  });
});
