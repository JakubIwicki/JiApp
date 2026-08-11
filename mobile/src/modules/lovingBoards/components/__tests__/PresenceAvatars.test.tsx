import React from 'react';
import { rtlRender } from '../../../../test/rtlUtils';
import PresenceAvatars from '../PresenceAvatars';

jest.mock('react-i18next', () => {
  const actual = jest.requireActual('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string) => key,
    }),
  };
});

describe('PresenceAvatars', () => {
  it('renders nothing when there are no users', () => {
    const { queryByTestId } = rtlRender(<PresenceAvatars userIds={[]} />);

    expect(queryByTestId('presence-avatars')).toBeNull();
  });

  it('renders a zero-padded avatar label for each visible user', () => {
    const { getByText } = rtlRender(<PresenceAvatars userIds={[5, 42, 100]} />);

    expect(getByText('#05')).toBeTruthy();
    expect(getByText('#42')).toBeTruthy();
    expect(getByText('#00')).toBeTruthy();
  });

  it('renders no overflow count at the three-avatar cap', () => {
    const { getByText, queryByText } = rtlRender(
      <PresenceAvatars userIds={[1, 2, 3]} />,
    );

    expect(getByText('#01')).toBeTruthy();
    expect(queryByText('+1')).toBeNull();
  });

  it('renders +1 overflow one past the cap and hides the hidden avatar', () => {
    const { getByText, queryByText } = rtlRender(
      <PresenceAvatars userIds={[1, 2, 3, 4]} />,
    );

    expect(getByText('+1')).toBeTruthy();
    expect(queryByText('#04')).toBeNull();
  });

  it('renders +2 overflow two past the cap', () => {
    const { getByText } = rtlRender(
      <PresenceAvatars userIds={[1, 2, 3, 4, 5]} />,
    );

    expect(getByText('+2')).toBeTruthy();
  });

  it('announces the online count to assistive tech and as the caption', () => {
    const { getByLabelText, getByText } = rtlRender(
      <PresenceAvatars userIds={[1, 2, 3, 4]} />,
    );

    expect(getByLabelText('lovingBoards.boardDetail.online')).toBeTruthy();
    expect(getByText('lovingBoards.boardDetail.online')).toBeTruthy();
  });
});
