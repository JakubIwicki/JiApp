import React from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { useTheme, useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { spacing } from '../styles/theme';

interface FormInputProps {
  value: string;
  onChangeText: (text: string) => void;
  placeholder?: string;
  secureTextEntry?: boolean;
  error?: string;
  label?: string;
  keyboardType?: 'default' | 'email-address';
  autoCapitalize?: 'none' | 'sentences' | 'words' | 'characters';
}

const FormInput: React.FC<FormInputProps> = ({
  value,
  onChangeText,
  placeholder,
  secureTextEntry = false,
  error,
  label,
  keyboardType = 'default',
  autoCapitalize,
}) => {
  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  return (
    <View style={styles.container}>
      {label && <Text style={styles.label}>{label}</Text>}
      <TextInput
        style={[styles.input, error ? styles.inputError : undefined]}
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        secureTextEntry={secureTextEntry}
        placeholderTextColor={colors.placeholderDark}
        keyboardType={keyboardType}
        autoCapitalize={autoCapitalize}
        testID="form-input"
      />
      {error && <Text style={styles.errorText}>{error}</Text>}
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      marginBottom: spacing.lg,
      width: '100%',
    },
    label: {
      fontSize: 14,
      fontWeight: '600',
      color: t.colors.textSecondary,
      marginBottom: 6,
    },
    input: {
      height: 48,
      borderWidth: 1,
      borderColor: t.colors.border,
      borderRadius: 8,
      paddingHorizontal: spacing.md,
      fontSize: 16,
      backgroundColor: t.colors.surface,
      color: t.colors.textPrimary,
    },
    inputError: {
      borderColor: t.colors.error,
    },
    errorText: {
      color: t.colors.error,
      fontSize: 12,
      marginTop: spacing.xs,
    },
  });

export default FormInput;
