import React, { useCallback, useEffect, useState } from 'react';
import {
  StyleSheet,
  Switch,
  Text,
  View,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { StackNavigationProp } from '@react-navigation/stack';
import type { AuthStackParamList } from '../navigation/types';
import AuthLayout from '../components/AuthLayout';
import FormInput from '../components/FormInput';
import useAuth from '../hooks/useAuth';
import useScreenTitle from '../hooks/useScreenTitle';
import * as storageService from '../services/storageService';
import { makeChangeHandler } from '../utils/formUtils';
import { colors, spacing } from '../styles/theme';

type LoginNavigationProp = StackNavigationProp<AuthStackParamList, 'Login'>;

const LoginScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<LoginNavigationProp>();
  const { login } = useAuth();

  useScreenTitle('auth.loginTitle');

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [usernameError, setUsernameError] = useState<string | undefined>();
  const [passwordError, setPasswordError] = useState<string | undefined>();
  const [apiError, setApiError] = useState<string | undefined>();
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    const loadSavedCredentials = async () => {
      const credentials = await storageService.getCredentials();
      if (credentials) {
        setUsername(credentials.username);
        setPassword(credentials.password);
        setRememberMe(true);
      }
    };
    loadSavedCredentials();
  }, []);

  const handleLogin = useCallback(async () => {
    setUsernameError(undefined);
    setPasswordError(undefined);
    setApiError(undefined);

    let hasError = false;

    if (!username.trim()) {
      setUsernameError(t('auth.usernameRequired'));
      hasError = true;
    }

    if (!password.trim()) {
      setPasswordError(t('auth.passwordRequired'));
      hasError = true;
    }

    if (hasError) return;

    setIsLoading(true);
    try {
      await login(username.trim(), password.trim());

      if (rememberMe) {
        const validUntil = new Date(
          Date.now() + 30 * 24 * 60 * 60 * 1000,
        ).toISOString();
        await storageService.saveCredentials({
          username: username.trim(),
          password: password.trim(),
          validUntil,
        });
      } else {
        await storageService.clearCredentials();
      }
    } catch {
      setApiError(t('auth.invalidCredentials'));
    } finally {
      setIsLoading(false);
    }
  }, [username, password, rememberMe, login, t]);

  const handleGoToRegister = useCallback(() => {
    navigation.navigate('Register');
  }, [navigation]);

  return (
    <AuthLayout
      title={t('auth.login')}
      buttonTitle={t('auth.login')}
      onButtonPress={handleLogin}
      buttonLoading={isLoading}
      apiError={apiError}
      footerLinkText={t('auth.goToRegister')}
      onFooterLinkPress={handleGoToRegister}
    >
      <FormInput
        value={username}
        onChangeText={makeChangeHandler(setUsername, () => setUsernameError(undefined))}
        placeholder={t('auth.username')}
        error={usernameError}
        autoCapitalize="none"
      />

      <FormInput
        value={password}
        onChangeText={makeChangeHandler(setPassword, () => setPasswordError(undefined))}
        placeholder={t('auth.password')}
        secureTextEntry={true}
        error={passwordError}
      />

      <View style={styles.rememberMeRow}>
        <Switch
          value={rememberMe}
          onValueChange={setRememberMe}
          testID="remember-me-switch"
          trackColor={{ false: colors.primaryLight, true: colors.primary }}
        />
        <Text style={styles.rememberMeLabel}>{t('auth.rememberMe')}</Text>
      </View>
    </AuthLayout>
  );
};

const styles = StyleSheet.create({
  rememberMeRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: spacing.lg,
  },
  rememberMeLabel: {
    fontSize: 14,
    color: colors.textTertiary,
    marginLeft: spacing.sm,
  },
});

export default LoginScreen;
