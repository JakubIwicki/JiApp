import React from 'react';
import { rtlRender } from '../../../../test/rtlUtils';
import ProgressStrip from '../ProgressStrip';

jest.mock('react-i18next', () => {
  const actual = jest.requireActual('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

describe('ProgressStrip', () => {
  it('renders nothing when total is zero so it cannot divide by zero', () => {
    const { queryByText } = rtlRender(<ProgressStrip done={0} total={0} />);

    expect(queryByText('lovingBoards.boardDetail.progress')).toBeNull();
  });

  it('renders the progress label for a partial count', () => {
    const { getByText } = rtlRender(<ProgressStrip done={3} total={5} />);

    expect(getByText('lovingBoards.boardDetail.progress')).toBeTruthy();
  });

  it('renders the progress label when nothing is done yet', () => {
    const { getByText } = rtlRender(<ProgressStrip done={0} total={5} />);

    expect(getByText('lovingBoards.boardDetail.progress')).toBeTruthy();
  });

  it('renders the progress label when done exceeds total', () => {
    const { getByText } = rtlRender(<ProgressStrip done={10} total={5} />);

    expect(getByText('lovingBoards.boardDetail.progress')).toBeTruthy();
  });
});
