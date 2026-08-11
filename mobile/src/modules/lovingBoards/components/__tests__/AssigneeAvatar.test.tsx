import React from 'react';
import { rtlRender } from '../../../../test/rtlUtils';
import AssigneeAvatar from '../AssigneeAvatar';

describe('AssigneeAvatar', () => {
  it('renders the decorative arrow and initials derived from the user id', () => {
    const { getByText } = rtlRender(<AssigneeAvatar userId={42} />);

    expect(getByText('→')).toBeTruthy();
    expect(getByText('42')).toBeTruthy();
  });

  it('renders a single-character initial for single-digit ids', () => {
    const { getByText } = rtlRender(<AssigneeAvatar userId={7} />);

    expect(getByText('7')).toBeTruthy();
  });

  it('uses the first two digits of longer ids', () => {
    const { getByText } = rtlRender(<AssigneeAvatar userId={123} />);

    expect(getByText('12')).toBeTruthy();
  });

  it('derives the same initials for the same id rendered twice', () => {
    const { getAllByText } = rtlRender(
      <>
        <AssigneeAvatar userId={42} />
        <AssigneeAvatar userId={42} />
      </>,
    );

    expect(getAllByText('42')).toHaveLength(2);
  });

  it('derives different initials for different ids', () => {
    const { getByText, queryByText } = rtlRender(
      <>
        <AssigneeAvatar userId={42} />
        <AssigneeAvatar userId={7} />
      </>,
    );

    expect(getByText('42')).toBeTruthy();
    expect(getByText('7')).toBeTruthy();
    expect(queryByText('12')).toBeNull();
  });
});
