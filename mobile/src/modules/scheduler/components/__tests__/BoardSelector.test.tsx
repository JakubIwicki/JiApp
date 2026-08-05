import React from 'react';
import { fireEvent } from '@testing-library/react-native';
import BoardSelector from '../BoardSelector';
import { BoardContext } from '../../../../context/BoardContext';
import { rtlRender } from '../../../../test/rtlUtils';

jest.mock('react-i18next', () => {
  const actual = jest.requireActual('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

const boardContextValue = {
  boards: [
    {
      id: 1,
      name: 'Salon Glamour',
      memberUserIds: [1, 2],
      createdAt: '2026-01-01T00:00:00.000Z',
    },
    {
      id: 2,
      name: 'Salon Kwiat',
      memberUserIds: [1],
      createdAt: '2026-02-01T00:00:00.000Z',
    },
  ],
  selectedBoardId: 1,
  isLoading: false,
  error: null,
  switchBoard: jest.fn(),
  loadBoards: jest.fn(),
  createBoard: jest.fn(),
  deleteBoard: jest.fn(),
  addMember: jest.fn(),
  removeMember: jest.fn(),
};

const renderBoardSelector = () =>
  rtlRender(
    <BoardContext.Provider value={boardContextValue}>
      <BoardSelector />
    </BoardContext.Provider>,
  );

describe('BoardSelector', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the selected board name in the selector', () => {
    const { getByText } = renderBoardSelector();

    expect(getByText('Salon Glamour ▾')).toBeTruthy();
  });

  it('keeps the board modal closed until the selector is pressed', () => {
    const { queryByText } = renderBoardSelector();

    expect(queryByText('boardManagement.title')).toBeNull();
  });

  it('lists every board and its member count when the modal opens', () => {
    const { getByText, getAllByText } = renderBoardSelector();

    fireEvent.press(getByText('Salon Glamour ▾'));

    expect(getByText('boardManagement.title')).toBeTruthy();
    expect(getByText('✓ Salon Glamour')).toBeTruthy();
    expect(getByText('Salon Kwiat')).toBeTruthy();
    expect(getAllByText('boardManagement.memberCount')).toHaveLength(2);
  });

  it('switches board and closes the modal when a board is chosen', () => {
    const { getByText, queryByText } = renderBoardSelector();

    fireEvent.press(getByText('Salon Glamour ▾'));
    fireEvent.press(getByText('Salon Kwiat'));

    expect(boardContextValue.switchBoard).toHaveBeenCalledWith(2);
    expect(queryByText('boardManagement.title')).toBeNull();
  });
});
