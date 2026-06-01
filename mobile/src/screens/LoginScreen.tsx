import React, { useCallback, useEffect, useReducer } from 'react';
import {
  StyleSheet,
  Switch,
  Text,
  View,
} from 'react-native';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { AuthStackParamList } from '../navigation/types';
import AuthLayout from '../components/AuthLayout';
import FormInput from '../components/FormInput';
import useAuth from '../hooks/useAuth';
import useScreenTitle from '../hooks/useScreenTitle';
import useToast from '../hooks/useToast';
import * as storageService from '../services/storageService';
import { colors, spacing } from '../styles/theme';

type LoginNavigationProp = NativeStackNavigationProp<AuthStackParamList, 'Login'>;

interface LoginFormState {
  username: string;
  password: string;
  rememberMe: boolean;
  usernameError: string | undefined;
  passwordError: string | undefined;
  apiError: string | undefined;
  isLoading: boolean;
}

type LoginFormAction =
  | { type: 'SET_FIELD'; field: 'username' | 'password'; value: string }
  | { type: 'SET_REMEMBER_ME'; value: boolean }
  | { type: 'SET_FIELD_ERROR'; field: 'username' | 'password'; error: string | undefined }
  | { type: 'SET_API_ERROR'; error: string | undefined }
  | { type: 'SET_LOADING'; loading: boolean }
  | { type: 'LOAD_CREDENTIALS'; username: string; password: string }
  | { type: 'CLEAR_ERRORS' };

const initialLoginFormState: LoginFormState = {
  username: '',
  password: '',
  rememberMe: false,
  usernameError: undefined,
  passwordError: undefined,
  apiError: undefined,
  isLoading: false,
};

function loginFormReducer(
  state: LoginFormState,
  action: LoginFormAction,
): LoginFormState {
  switch (action.type) {
    case 'SET_FIELD':
      return { ...state, [action.field]: action.value };
    case 'SET_REMEMBER_ME':
      return { ...state, rememberMe: action.value };
    case 'SET_FIELD_ERROR':
      return { ...state, [`${action.field}Error`]: action.error };
    case 'SET_API_ERROR':
      return { ...state, apiError: action.error };
    case 'SET_LOADING':
      return { ...state, isLoading: action.loading };
    case 'LOAD_CREDENTIALS':
      return { ...state, username: action.username, password: action.password, rememberMe: true };
    case 'CLEAR_ERRORS':
      return {
        ...state,
        usernameError: undefined,
        passwordError: undefined,
        apiError: undefined,
      };
    default:
      return state;
  }
}

const LoginScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<LoginNavigationProp>();
  const { login } = useAuth();
  const { showInfo } = useToast();

  useScreenTitle('auth.loginTitle');

  const [form, dispatch] = useReducer(loginFormReducer, initialLoginFormState);

  // Initialize saved credentials on mount
  useEffect(() => {
    storageService.getCredentials().then((credentials) => {
      if (credentials) {
        dispatch({ type: 'LOAD_CREDENTIALS', username: credentials.username, password: credentials.password });
      }
    }).catch(() => {});
  }, []); // mount only

  const handleLogin = useCallback(async () => {
    dispatch({ type: 'CLEAR_ERRORS' });

    let hasError = false;

    if (!form.username.trim()) {
      dispatch({ type: 'SET_FIELD_ERROR', field: 'username', error: t('auth.usernameRequired') });
      hasError = true;
    }

    if (!form.password.trim()) {
      dispatch({ type: 'SET_FIELD_ERROR', field: 'password', error: t('auth.passwordRequired') });
      hasError = true;
    }

    if (hasError) return;

    dispatch({ type: 'SET_LOADING', loading: true });
    try {
      await login(form.username.trim(), form.password.trim());
      showInfo('toast.loggedIn');

      if (form.rememberMe) {
        const validUntil = new Date(
          Date.now() + 30 * 24 * 60 * 60 * 1000,
        ).toISOString();
        await storageService.saveCredentials({
          username: form.username.trim(),
          password: form.password.trim(),
          validUntil,
        });
      } else {
        await storageService.clearCredentials();
      }
    } catch {
      dispatch({ type: 'SET_API_ERROR', error: t('auth.invalidCredentials') });
    } finally {
      dispatch({ type: 'SET_LOADING', loading: false });
    }
  }, [form.username, form.password, form.rememberMe, login, t, showInfo]);

  const handleGoToRegister = useCallback(() => {
    navigation.navigate('Register');
  }, [navigation]);

  return (
    <AuthLayout
      title={t('auth.login')}
      buttonTitle={t('auth.login')}
      onButtonPress={handleLogin}
      buttonLoading={form.isLoading}
      apiError={form.apiError}
      footerLinkText={t('auth.goToRegister')}
      onFooterLinkPress={handleGoToRegister}
    >
      <FormInput
        value={form.username}
        onChangeText={(text) => {
          dispatch({ type: 'SET_FIELD', field: 'username', value: text });
          dispatch({ type: 'SET_FIELD_ERROR', field: 'username', error: undefined });
        }}
        placeholder={t('auth.username')}
        error={form.usernameError}
        autoCapitalize="none"
      />

      <FormInput
        value={form.password}
        onChangeText={(text) => {
          dispatch({ type: 'SET_FIELD', field: 'password', value: text });
          dispatch({ type: 'SET_FIELD_ERROR', field: 'password', error: undefined });
        }}
        placeholder={t('auth.password')}
        secureTextEntry={true}
        error={form.passwordError}
      />

      <View style={styles.rememberMeRow}>
        <Switch
          value={form.rememberMe}
          onValueChange={(value) => dispatch({ type: 'SET_REMEMBER_ME', value })}
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
