import React from 'react';
import { render } from '@testing-library/react-native';
import RefreshableScrollView from '../RefreshableScrollView';

describe('RefreshableScrollView', () => {
  it('defaults keyboardShouldPersistTaps to handled', () => {
    const { getByTestId } = render(
      <RefreshableScrollView
        refreshing={false}
        onRefresh={jest.fn()}
        testID="scroll"
      />,
    );

    const scrollView = getByTestId('scroll');
    expect(scrollView.props.keyboardShouldPersistTaps).toBe('handled');
  });

  it('lets a caller-supplied keyboardShouldPersistTaps win', () => {
    const { getByTestId } = render(
      <RefreshableScrollView
        refreshing={false}
        onRefresh={jest.fn()}
        testID="scroll"
        keyboardShouldPersistTaps="never"
      />,
    );

    const scrollView = getByTestId('scroll');
    expect(scrollView.props.keyboardShouldPersistTaps).toBe('never');
  });
});
