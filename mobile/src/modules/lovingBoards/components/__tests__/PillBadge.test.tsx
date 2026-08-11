import React from 'react';
import { rtlRender } from '../../../../test/rtlUtils';
import PillBadge from '../PillBadge';

const VARIANTS = ['default', 'recurring', 'warning', 'error'] as const;

describe('PillBadge', () => {
  it('renders its text', () => {
    const { getByText } = rtlRender(<PillBadge text="×2" />);

    expect(getByText('×2')).toBeTruthy();
  });

  it('defaults to the default variant when none is given', () => {
    const { getByText } = rtlRender(<PillBadge text="Pill" />);

    expect(getByText('Pill')).toBeTruthy();
  });

  it('renders every variant', () => {
    for (const variant of VARIANTS) {
      const { getByText } = rtlRender(
        <PillBadge text="Pill" variant={variant} />,
      );

      expect(getByText('Pill')).toBeTruthy();
    }
  });

  it('exposes the accessibility label', () => {
    const { getByLabelText } = rtlRender(
      <PillBadge text="🔁" accessibilityLabel="Recurring" />,
    );

    expect(getByLabelText('Recurring')).toBeTruthy();
  });
});
