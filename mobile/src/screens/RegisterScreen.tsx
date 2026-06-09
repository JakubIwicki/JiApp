import React, { useCallback, useReducer } from 'react';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import axios from 'axios';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import type { AuthStackParamList } from '../navigation/types';
import AuthLayout from '../components/AuthLayout';
import FormInput from '../components/FormInput';
import useAuth from '../hooks/useAuth';
import useScreenTitle from '../hooks/useScreenTitle';
import useToast from '../hooks/useToast';

type RegisterNavigationProp = NativeStackNavigationProp<
  AuthStackParamList,
  'Register'
>;

const PASSWORD_MIN_LENGTH = 8;

/**
 * Maps server validation error messages to form field names.
 * Each message mentions one field (Password, Username, Email, DisplayName).
 */
function extractFieldErrors(
  serverErrors: string[],
): Partial<Record<string, string>> {
  const fieldMap: Record<string, string> = {
    password: 'password',
    username: 'username',
    email: 'email',
    displayname: 'displayName',
  };

  const entries = Object.entries(fieldMap);
  const result: Partial<Record<string, string>> = {};

  for (const msg of serverErrors) {
    for (const [key, field] of entries) {
      if (msg.toLowerCase().includes(key)) {
        result[field] = msg;
        break;
      }
    }
  }

  return result;
}

interface RegisterFormState {
  username: string;
  email: string;
  password: string;
  displayName: string;
  usernameError: string | undefined;
  emailError: string | undefined;
  passwordError: string | undefined;
  displayNameError: string | undefined;
  apiError: string | undefined;
  isLoading: boolean;
}

type RegisterFormAction =
  | {
      type: 'SET_FIELD';
      field: 'username' | 'email' | 'password' | 'displayName';
      value: string;
    }
  | {
      type: 'SET_FIELD_ERROR';
      field: 'username' | 'email' | 'password' | 'displayName';
      error: string | undefined;
    }
  | { type: 'SET_API_ERROR'; error: string | undefined }
  | { type: 'SET_LOADING'; loading: boolean }
  | { type: 'CLEAR_ERRORS' };

const initialRegisterFormState: RegisterFormState = {
  username: '',
  email: '',
  password: '',
  displayName: '',
  usernameError: undefined,
  emailError: undefined,
  passwordError: undefined,
  displayNameError: undefined,
  apiError: undefined,
  isLoading: false,
};

function registerFormReducer(
  state: RegisterFormState,
  action: RegisterFormAction,
): RegisterFormState {
  switch (action.type) {
    case 'SET_FIELD':
      return { ...state, [action.field]: action.value };
    case 'SET_FIELD_ERROR':
      return { ...state, [`${action.field}Error`]: action.error };
    case 'SET_API_ERROR':
      return { ...state, apiError: action.error };
    case 'SET_LOADING':
      return { ...state, isLoading: action.loading };
    case 'CLEAR_ERRORS':
      return {
        ...state,
        usernameError: undefined,
        emailError: undefined,
        passwordError: undefined,
        displayNameError: undefined,
        apiError: undefined,
      };
    default:
      return state;
  }
}

const RegisterScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<RegisterNavigationProp>();
  const { register } = useAuth();
  const { showSuccess } = useToast();

  useScreenTitle('auth.registerTitle');

  const [form, dispatch] = useReducer(
    registerFormReducer,
    initialRegisterFormState,
  );

  const validate = useCallback((): boolean => {
    dispatch({ type: 'CLEAR_ERRORS' });
    let hasError = false;

    if (!form.username.trim()) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'username',
        error: t('auth.usernameRequired'),
      });
      hasError = true;
    } else if (form.username.trim().length < 3) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'username',
        error: t('auth.usernameTooShort'),
      });
      hasError = true;
    }

    if (!form.email.trim()) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'email',
        error: t('auth.emailRequired'),
      });
      hasError = true;
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim())) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'email',
        error: t('auth.invalidEmail'),
      });
      hasError = true;
    }

    if (!form.password.trim()) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'password',
        error: t('auth.passwordRequired'),
      });
      hasError = true;
    } else if (form.password.trim().length < PASSWORD_MIN_LENGTH) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'password',
        error: t('auth.passwordTooShort'),
      });
      hasError = true;
    } else if (!/[A-Z]/.test(form.password)) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'password',
        error: t('auth.passwordNoUppercase'),
      });
      hasError = true;
    } else if (!/[0-9]/.test(form.password)) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'password',
        error: t('auth.passwordNoDigit'),
      });
      hasError = true;
    }

    if (!form.displayName.trim()) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'displayName',
        error: t('auth.displayNameRequired'),
      });
      hasError = true;
    }

    return !hasError;
  }, [form.username, form.email, form.password, form.displayName, t]);

  const handleRegister = useCallback(async () => {
    if (!validate()) return;

    dispatch({ type: 'SET_LOADING', loading: true });
    try {
      await register(
        form.username.trim(),
        form.email.trim(),
        form.password.trim(),
        form.displayName.trim(),
      );
      showSuccess('toast.registerSuccess');
      navigation.navigate('Login');
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response?.status === 400) {
        const serverErrors: string[] = err.response?.data?.errors?.errors ?? [];
        if (serverErrors.length > 0) {
          const fieldErrors = extractFieldErrors(serverErrors);
          for (const [field, error] of Object.entries(fieldErrors)) {
            dispatch({
              type: 'SET_FIELD_ERROR',
              field: field as 'username' | 'email' | 'password' | 'displayName',
              error,
            });
          }
          return;
        }
      }
      dispatch({ type: 'SET_API_ERROR', error: t('auth.registerFailed') });
    } finally {
      dispatch({ type: 'SET_LOADING', loading: false });
    }
  }, [
    form.username,
    form.email,
    form.password,
    form.displayName,
    register,
    navigation,
    t,
    validate,
    showSuccess,
  ]);

  const handleGoToLogin = useCallback(() => {
    navigation.navigate('Login');
  }, [navigation]);

  return (
    <AuthLayout
      title={t('auth.register')}
      buttonTitle={t('auth.register')}
      onButtonPress={handleRegister}
      buttonLoading={form.isLoading}
      apiError={form.apiError}
      footerLinkText={t('auth.goToLogin')}
      onFooterLinkPress={handleGoToLogin}
    >
      <FormInput
        value={form.username}
        onChangeText={text => {
          dispatch({ type: 'SET_FIELD', field: 'username', value: text });
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: 'username',
            error: undefined,
          });
        }}
        placeholder={t('auth.username')}
        error={form.usernameError}
        autoCapitalize="none"
      />

      <FormInput
        value={form.email}
        onChangeText={text => {
          dispatch({ type: 'SET_FIELD', field: 'email', value: text });
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: 'email',
            error: undefined,
          });
        }}
        placeholder={t('auth.email')}
        error={form.emailError}
        keyboardType="email-address"
        autoCapitalize="none"
      />

      <FormInput
        value={form.password}
        onChangeText={text => {
          dispatch({ type: 'SET_FIELD', field: 'password', value: text });
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: 'password',
            error: undefined,
          });
        }}
        placeholder={t('auth.password')}
        secureTextEntry={true}
        error={form.passwordError}
      />

      <FormInput
        value={form.displayName}
        onChangeText={text => {
          dispatch({ type: 'SET_FIELD', field: 'displayName', value: text });
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: 'displayName',
            error: undefined,
          });
        }}
        placeholder={t('auth.displayName')}
        error={form.displayNameError}
      />
    </AuthLayout>
  );
};

export default RegisterScreen;
