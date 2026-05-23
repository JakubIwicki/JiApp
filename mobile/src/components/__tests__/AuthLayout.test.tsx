import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import { Text } from 'react-native';
import AuthLayout from '../AuthLayout';

describe('AuthLayout', () => {
  const defaultProps = {
    title: 'Test Title',
    buttonTitle: 'Submit',
    onButtonPress: jest.fn(),
    footerLinkText: 'Go to login',
    onFooterLinkPress: jest.fn(),
  };

  it('renders title and children', () => {
    const { getByText } = render(
      <AuthLayout {...defaultProps}>
        <Text>Child Content</Text>
      </AuthLayout>,
    );
    expect(getByText('Test Title')).toBeTruthy();
    expect(getByText('Child Content')).toBeTruthy();
  });

  it('renders button with correct title', () => {
    const { getByTestId } = render(
      <AuthLayout {...defaultProps}>
        <Text>Child</Text>
      </AuthLayout>,
    );
    const button = getByTestId('button');
    expect(button).toBeTruthy();
  });

  it('button shows loading state', () => {
    const { getByTestId } = render(
      <AuthLayout {...defaultProps} buttonLoading={true}>
        <Text>Child</Text>
      </AuthLayout>,
    );
    expect(getByTestId('button-loading')).toBeTruthy();
  });

  it('renders apiError when provided', () => {
    const { getByText } = render(
      <AuthLayout {...defaultProps} apiError="Something went wrong">
        <Text>Child</Text>
      </AuthLayout>,
    );
    expect(getByText('Something went wrong')).toBeTruthy();
  });

  it('does not render apiError when undefined', () => {
    const { queryByText } = render(
      <AuthLayout {...defaultProps}>
        <Text>Child</Text>
      </AuthLayout>,
    );
    expect(queryByText('Something went wrong')).toBeNull();
  });

  it('footer link is tappable', () => {
    const onPress = jest.fn();
    const { getByText } = render(
      <AuthLayout {...defaultProps} onFooterLinkPress={onPress}>
        <Text>Child</Text>
      </AuthLayout>,
    );
    fireEvent.press(getByText('Go to login'));
    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('footer link text renders', () => {
    const { getByText } = render(
      <AuthLayout {...defaultProps} footerLinkText="Create account">
        <Text>Child</Text>
      </AuthLayout>,
    );
    expect(getByText('Create account')).toBeTruthy();
  });

  it('calls onButtonPress when button is pressed', () => {
    const onPress = jest.fn();
    const { getByTestId } = render(
      <AuthLayout {...defaultProps} onButtonPress={onPress}>
        <Text>Child</Text>
      </AuthLayout>,
    );
    fireEvent.press(getByTestId('button'));
    expect(onPress).toHaveBeenCalledTimes(1);
  });
});
