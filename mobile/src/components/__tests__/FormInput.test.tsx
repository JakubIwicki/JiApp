import React from 'react';
import { render, fireEvent } from '@testing-library/react-native';
import FormInput from '../FormInput';

describe('FormInput', () => {
  it('renders with placeholder', () => {
    const { getByPlaceholderText } = render(
      <FormInput value="" onChangeText={jest.fn()} placeholder="Username" />,
    );
    expect(getByPlaceholderText('Username')).toBeTruthy();
  });

  it('calls onChangeText on text input', () => {
    const onChangeText = jest.fn();
    const { getByPlaceholderText } = render(
      <FormInput
        value=""
        onChangeText={onChangeText}
        placeholder="Username"
      />,
    );
    fireEvent.changeText(getByPlaceholderText('Username'), 'john');
    expect(onChangeText).toHaveBeenCalledWith('john');
  });

  it('shows error message when error prop is set', () => {
    const { getByText } = render(
      <FormInput
        value=""
        onChangeText={jest.fn()}
        placeholder="Username"
        error="Required"
      />,
    );
    expect(getByText('Required')).toBeTruthy();
  });

  it('applies error border color when error prop is set', () => {
    const { getByPlaceholderText } = render(
      <FormInput
        value=""
        onChangeText={jest.fn()}
        placeholder="Username"
        error="Required"
      />,
    );
    const input = getByPlaceholderText('Username');
    const styles = input.props.style;
    const flatStyles = Array.isArray(styles) ? styles : [styles];
    const hasErrorBorder = flatStyles.some(
      (s: Record<string, unknown>) => s.borderColor === '#C1440E',
    );
    expect(hasErrorBorder).toBe(true);
  });

  it('shows label when provided', () => {
    const { getByText } = render(
      <FormInput
        value=""
        onChangeText={jest.fn()}
        placeholder="Username"
        label="Username Label"
      />,
    );
    expect(getByText('Username Label')).toBeTruthy();
  });

  it('toggles secureTextEntry for password variant', () => {
    const { getByPlaceholderText } = render(
      <FormInput
        value=""
        onChangeText={jest.fn()}
        placeholder="Password"
        secureTextEntry={true}
      />,
    );
    const input = getByPlaceholderText('Password');
    expect(input.props.secureTextEntry).toBe(true);
  });

  it('uses wabi-sabi placeholder color', () => {
    const { getByPlaceholderText } = render(
      <FormInput value="" onChangeText={jest.fn()} placeholder="Username" />,
    );
    const input = getByPlaceholderText('Username');
    expect(input.props.placeholderTextColor).toBe('#DDD6CE');
  });
});
