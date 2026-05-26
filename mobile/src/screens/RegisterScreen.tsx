import React, { useCallback, useState } from 'react';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import type { StackNavigationProp } from '@react-navigation/stack';
import type { AuthStackParamList } from '../navigation/types';
import AuthLayout from '../components/AuthLayout';
import FormInput from '../components/FormInput';
import useAuth from '../hooks/useAuth';
import useScreenTitle from '../hooks/useScreenTitle';
import useToast from '../hooks/useToast';
import { makeChangeHandler } from '../utils/formUtils';

type RegisterNavigationProp = StackNavigationProp<
  AuthStackParamList,
  'Register'
>;

const RegisterScreen: React.FC = () => {
  const { t } = useTranslation();
  const navigation = useNavigation<RegisterNavigationProp>();
  const { register } = useAuth();
  const { showSuccess } = useToast();

  useScreenTitle('auth.registerTitle');

  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');

  const [usernameError, setUsernameError] = useState<string | undefined>();
  const [emailError, setEmailError] = useState<string | undefined>();
  const [passwordError, setPasswordError] = useState<string | undefined>();
  const [displayNameError, setDisplayNameError] = useState<
    string | undefined
  >();
  const [apiError, setApiError] = useState<string | undefined>();
  const [isLoading, setIsLoading] = useState(false);

  const clearErrors = useCallback(() => {
    setUsernameError(undefined);
    setEmailError(undefined);
    setPasswordError(undefined);
    setDisplayNameError(undefined);
    setApiError(undefined);
  }, []);

  const validate = useCallback((): boolean => {
    clearErrors();
    let hasError = false;

    if (!username.trim()) {
      setUsernameError(t('auth.usernameRequired'));
      hasError = true;
    } else if (username.trim().length < 3) {
      setUsernameError(t('auth.usernameTooShort'));
      hasError = true;
    }

    if (!email.trim()) {
      setEmailError(t('auth.emailRequired'));
      hasError = true;
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      setEmailError(t('auth.invalidEmail'));
      hasError = true;
    }

    if (!password.trim()) {
      setPasswordError(t('auth.passwordRequired'));
      hasError = true;
    } else if (password.trim().length < 4) {
      setPasswordError(t('auth.passwordTooShort'));
      hasError = true;
    }

    if (!displayName.trim()) {
      setDisplayNameError(t('auth.displayNameRequired'));
      hasError = true;
    }

    return !hasError;
  }, [username, email, password, displayName, t, clearErrors]);

  const handleRegister = useCallback(async () => {
    if (!validate()) return;

    setIsLoading(true);
    try {
      await register(
        username.trim(),
        email.trim(),
        password.trim(),
        displayName.trim(),
      );
      showSuccess('toast.registerSuccess');
      navigation.navigate('Login');
    } catch {
      setApiError(t('auth.registerFailed'));
    } finally {
      setIsLoading(false);
    }
  }, [
    username,
    email,
    password,
    displayName,
    register,
    navigation,
    t,
    validate,
  ]);

  const handleGoToLogin = useCallback(() => {
    navigation.navigate('Login');
  }, [navigation]);

  return (
    <AuthLayout
      title={t('auth.register')}
      buttonTitle={t('auth.register')}
      onButtonPress={handleRegister}
      buttonLoading={isLoading}
      apiError={apiError}
      footerLinkText={t('auth.goToLogin')}
      onFooterLinkPress={handleGoToLogin}
    >
      <FormInput
        value={username}
        onChangeText={makeChangeHandler(setUsername, () => setUsernameError(undefined))}
        placeholder={t('auth.username')}
        error={usernameError}
        autoCapitalize="none"
      />

      <FormInput
        value={email}
        onChangeText={makeChangeHandler(setEmail, () => setEmailError(undefined))}
        placeholder={t('auth.email')}
        error={emailError}
        keyboardType="email-address"
        autoCapitalize="none"
      />

      <FormInput
        value={password}
        onChangeText={makeChangeHandler(setPassword, () => setPasswordError(undefined))}
        placeholder={t('auth.password')}
        secureTextEntry={true}
        error={passwordError}
      />

      <FormInput
        value={displayName}
        onChangeText={makeChangeHandler(setDisplayName, () => setDisplayNameError(undefined))}
        placeholder={t('auth.displayName')}
        error={displayNameError}
      />
    </AuthLayout>
  );
};

export default RegisterScreen;
