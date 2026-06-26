import React from 'react';
import {
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  Text,
} from 'react-native';
import Button from './Button';
import { useTheme, useThemedStyles } from '../context/ThemeContext';
import type { Theme } from '../styles/theme';
import { spacing } from '../styles/theme';
import { StyleSheet } from 'react-native';

interface AuthLayoutProps {
  title: string;
  children: React.ReactNode;
  apiError?: string;
  buttonTitle: string;
  onButtonPress: () => void;
  buttonLoading?: boolean;
  footerLinkText: string;
  onFooterLinkPress: () => void;
}

const AuthLayout: React.FC<AuthLayoutProps> = ({
  title,
  children,
  apiError,
  buttonTitle,
  onButtonPress,
  buttonLoading = false,
  footerLinkText,
  onFooterLinkPress,
}) => {
  const { commonStyles } = useTheme();
  const styles = useThemedStyles(makeStyles);
  return (
    <KeyboardAvoidingView
      style={commonStyles.screenContainer}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView
        contentContainerStyle={commonStyles.authScrollContent}
        keyboardShouldPersistTaps="handled"
      >
        <Text style={styles.title}>{title}</Text>

        {children}

        {apiError && <Text style={commonStyles.apiError}>{apiError}</Text>}

        <Button
          title={buttonTitle}
          onPress={onButtonPress}
          loading={buttonLoading}
        />

        <Pressable
          onPress={onFooterLinkPress}
          style={({ pressed }) => [
            commonStyles.linkContainer,
            pressed && { opacity: 0.7 },
          ]}
        >
          <Text style={commonStyles.linkText}>{footerLinkText}</Text>
        </Pressable>
      </ScrollView>
    </KeyboardAvoidingView>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    title: {
      ...t.typography.title,
      marginBottom: spacing.xxl,
      textAlign: 'center',
    },
  });

export default AuthLayout;
