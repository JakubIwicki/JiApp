import React, { useCallback, useEffect, useReducer, useRef } from 'react';
import { ScrollView, Text } from 'react-native';
import { useTranslation } from 'react-i18next';
import axios from 'axios';
import * as authService from '../services/authService';
import ProfileSection from './ProfileSection';
import ChangePasswordSection from './ChangePasswordSection';
import useAuth from '../hooks/useAuth';
import useScreenTitle from '../hooks/useScreenTitle';
import useToast from '../hooks/useToast';
import { useTheme } from '../context/ThemeContext';

const PASSWORD_MIN_LENGTH = 8;
const DISPLAY_NAME_MAX_LENGTH = 50;

/**
 * Maps server validation error messages to form field names.
 */
function extractFieldErrors(
  serverErrors: string[],
): Partial<Record<string, string>> {
  const fieldMap: Record<string, string> = {
    currentpassword: 'currentPassword',
    newpassword: 'newPassword',
    password: 'currentPassword',
    displayname: 'displayName',
    email: 'email',
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

interface EditProfileFormState {
  displayName: string;
  email: string;
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
  displayNameError: string | undefined;
  emailError: string | undefined;
  currentPasswordError: string | undefined;
  newPasswordError: string | undefined;
  confirmPasswordError: string | undefined;
  apiError: string | undefined;
  profileLoading: boolean;
  passwordLoading: boolean;
  initialized: boolean;
}

type EditProfileFormAction =
  | {
      type: 'SET_FIELD';
      field:
        | 'displayName'
        | 'email'
        | 'currentPassword'
        | 'newPassword'
        | 'confirmPassword';
      value: string;
    }
  | {
      type: 'SET_FIELD_ERROR';
      field:
        | 'displayName'
        | 'email'
        | 'currentPassword'
        | 'newPassword'
        | 'confirmPassword';
      error: string | undefined;
    }
  | { type: 'SET_API_ERROR'; error: string | undefined }
  | { type: 'SET_PROFILE_LOADING'; loading: boolean }
  | { type: 'SET_PASSWORD_LOADING'; loading: boolean }
  | {
      type: 'INIT_FIELDS';
      displayName: string;
      email: string;
    }
  | { type: 'CLEAR_PASSWORD_FIELDS' }
  | { type: 'CLEAR_PROFILE_ERRORS' }
  | { type: 'CLEAR_PASSWORD_ERRORS' };

const initialEditProfileFormState: EditProfileFormState = {
  displayName: '',
  email: '',
  currentPassword: '',
  newPassword: '',
  confirmPassword: '',
  displayNameError: undefined,
  emailError: undefined,
  currentPasswordError: undefined,
  newPasswordError: undefined,
  confirmPasswordError: undefined,
  apiError: undefined,
  profileLoading: false,
  passwordLoading: false,
  initialized: false,
};

function editProfileFormReducer(
  state: EditProfileFormState,
  action: EditProfileFormAction,
): EditProfileFormState {
  switch (action.type) {
    case 'SET_FIELD':
      return { ...state, [action.field]: action.value };
    case 'SET_FIELD_ERROR':
      return { ...state, [`${action.field}Error`]: action.error };
    case 'SET_API_ERROR':
      return { ...state, apiError: action.error };
    case 'SET_PROFILE_LOADING':
      return { ...state, profileLoading: action.loading };
    case 'SET_PASSWORD_LOADING':
      return { ...state, passwordLoading: action.loading };
    case 'INIT_FIELDS':
      return {
        ...state,
        displayName: action.displayName,
        email: action.email,
        initialized: true,
      };
    case 'CLEAR_PASSWORD_FIELDS':
      return {
        ...state,
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
        currentPasswordError: undefined,
        newPasswordError: undefined,
        confirmPasswordError: undefined,
      };
    case 'CLEAR_PROFILE_ERRORS':
      return {
        ...state,
        displayNameError: undefined,
        emailError: undefined,
        apiError: undefined,
      };
    case 'CLEAR_PASSWORD_ERRORS':
      return {
        ...state,
        currentPasswordError: undefined,
        newPasswordError: undefined,
        confirmPasswordError: undefined,
        apiError: undefined,
      };
    default:
      return state;
  }
}

const EditProfileScreen: React.FC = () => {
  const { t } = useTranslation();
  const { updateProfile } = useAuth();
  const { showSuccess } = useToast();

  useScreenTitle('settings.editProfile');
  const { commonStyles } = useTheme();

  const [form, dispatch] = useReducer(
    editProfileFormReducer,
    initialEditProfileFormState,
  );

  const mountedRef = useRef(true);

  useEffect(() => {
    mountedRef.current = true;
    return () => {
      mountedRef.current = false;
    };
  }, []);

  // Prefill displayName + email from server on mount
  useEffect(() => {
    let cancelled = false;

    const loadProfile = async () => {
      try {
        const profile = await authService.getProfile();
        if (!cancelled) {
          dispatch({
            type: 'INIT_FIELDS',
            displayName: profile.displayName,
            email: profile.email ?? '',
          });
        }
      } catch {
        // Silently ignore — user can type fields manually
        if (!cancelled) {
          dispatch({ type: 'INIT_FIELDS', displayName: '', email: '' });
        }
      }
    };

    loadProfile();

    return () => {
      cancelled = true;
    };
  }, []);

  // --- Profile section validation ---

  const validateProfile = useCallback((): boolean => {
    dispatch({ type: 'CLEAR_PROFILE_ERRORS' });
    let hasError = false;

    if (!form.displayName.trim()) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'displayName',
        error: t('auth.displayNameRequired'),
      });
      hasError = true;
    } else if (form.displayName.trim().length > DISPLAY_NAME_MAX_LENGTH) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'displayName',
        error: t('settings.displayNameMax'),
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

    return !hasError;
  }, [form.displayName, form.email, t]);

  const handleSaveProfile = useCallback(async () => {
    if (!validateProfile()) return;

    dispatch({ type: 'SET_PROFILE_LOADING', loading: true });
    try {
      await updateProfile(form.displayName.trim(), form.email.trim());
      showSuccess('settings.profileUpdated');
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response?.status === 400) {
        const serverError: string | undefined = err.response?.data?.error;
        const validationErrors: string[] =
          err.response?.data?.errors?.errors ?? [];

        if (validationErrors.length > 0) {
          const fieldErrors = extractFieldErrors(validationErrors);
          for (const [field, error] of Object.entries(fieldErrors)) {
            dispatch({
              type: 'SET_FIELD_ERROR',
              field: field as
                | 'displayName'
                | 'email'
                | 'currentPassword'
                | 'newPassword'
                | 'confirmPassword',
              error,
            });
          }
          dispatch({ type: 'SET_PROFILE_LOADING', loading: false });
          return;
        }

        if (serverError) {
          dispatch({ type: 'SET_API_ERROR', error: serverError });
          dispatch({ type: 'SET_PROFILE_LOADING', loading: false });
          return;
        }
      }

      if (axios.isAxiosError(err) && err.response?.status === 409) {
        dispatch({
          type: 'SET_FIELD_ERROR',
          field: 'email',
          error: t('settings.emailTaken'),
        });
        return;
      }

      dispatch({ type: 'SET_API_ERROR', error: t('common.error') });
    } finally {
      if (mountedRef.current) {
        dispatch({ type: 'SET_PROFILE_LOADING', loading: false });
      }
    }
  }, [
    form.displayName,
    form.email,
    updateProfile,
    showSuccess,
    t,
    validateProfile,
  ]);

  // --- Password section validation ---

  const validatePassword = useCallback((): boolean => {
    // Clear only password-related errors (plus apiError)
    dispatch({ type: 'CLEAR_PASSWORD_ERRORS' });
    let hasError = false;

    if (!form.currentPassword.trim()) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'currentPassword',
        error: t('auth.passwordRequired'),
      });
      hasError = true;
    }

    if (!form.newPassword.trim()) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'newPassword',
        error: t('auth.passwordRequired'),
      });
      hasError = true;
    } else if (form.newPassword.trim().length < PASSWORD_MIN_LENGTH) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'newPassword',
        error: t('auth.passwordTooShort'),
      });
      hasError = true;
    }

    if (!form.confirmPassword.trim()) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'confirmPassword',
        error: t('auth.passwordRequired'),
      });
      hasError = true;
    } else if (form.newPassword !== form.confirmPassword) {
      dispatch({
        type: 'SET_FIELD_ERROR',
        field: 'confirmPassword',
        error: t('settings.passwordsMustMatch'),
      });
      hasError = true;
    }

    return !hasError;
  }, [form.currentPassword, form.newPassword, form.confirmPassword, t]);

  const handleSavePassword = useCallback(async () => {
    if (!validatePassword()) return;

    dispatch({ type: 'SET_PASSWORD_LOADING', loading: true });
    try {
      await authService.changePassword(
        form.currentPassword.trim(),
        form.newPassword.trim(),
      );
      showSuccess('settings.passwordChanged');
      dispatch({ type: 'CLEAR_PASSWORD_FIELDS' });
    } catch (err: unknown) {
      if (axios.isAxiosError(err) && err.response?.status === 400) {
        const serverError: string | undefined = err.response?.data?.error;
        const validationErrors: string[] =
          err.response?.data?.errors?.errors ?? [];

        if (validationErrors.length > 0) {
          const fieldErrors = extractFieldErrors(validationErrors);
          for (const [field, error] of Object.entries(fieldErrors)) {
            dispatch({
              type: 'SET_FIELD_ERROR',
              field: field as
                | 'displayName'
                | 'email'
                | 'currentPassword'
                | 'newPassword'
                | 'confirmPassword',
              error,
            });
          }
          dispatch({ type: 'SET_PASSWORD_LOADING', loading: false });
          return;
        }

        if (serverError) {
          dispatch({ type: 'SET_API_ERROR', error: serverError });
          dispatch({ type: 'SET_PASSWORD_LOADING', loading: false });
          return;
        }
      }
      dispatch({ type: 'SET_API_ERROR', error: t('common.error') });
    } finally {
      if (mountedRef.current) {
        dispatch({ type: 'SET_PASSWORD_LOADING', loading: false });
      }
    }
  }, [
    form.currentPassword,
    form.newPassword,
    showSuccess,
    t,
    validatePassword,
  ]);

  return (
    <ScrollView
      style={commonStyles.screenContainer}
      contentContainerStyle={commonStyles.scrollContent}
    >
      <ProfileSection
        displayName={form.displayName}
        email={form.email}
        displayNameError={form.displayNameError}
        emailError={form.emailError}
        loading={form.profileLoading}
        onDisplayNameChange={text => {
          dispatch({ type: 'SET_FIELD', field: 'displayName', value: text });
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: 'displayName',
            error: undefined,
          });
        }}
        onEmailChange={text => {
          dispatch({ type: 'SET_FIELD', field: 'email', value: text });
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: 'email',
            error: undefined,
          });
        }}
        onSave={handleSaveProfile}
        t={t}
      />

      <ChangePasswordSection
        currentPassword={form.currentPassword}
        newPassword={form.newPassword}
        confirmPassword={form.confirmPassword}
        currentPasswordError={form.currentPasswordError}
        newPasswordError={form.newPasswordError}
        confirmPasswordError={form.confirmPasswordError}
        loading={form.passwordLoading}
        onCurrentPasswordChange={text => {
          dispatch({
            type: 'SET_FIELD',
            field: 'currentPassword',
            value: text,
          });
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: 'currentPassword',
            error: undefined,
          });
        }}
        onNewPasswordChange={text => {
          dispatch({
            type: 'SET_FIELD',
            field: 'newPassword',
            value: text,
          });
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: 'newPassword',
            error: undefined,
          });
        }}
        onConfirmPasswordChange={text => {
          dispatch({
            type: 'SET_FIELD',
            field: 'confirmPassword',
            value: text,
          });
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: 'confirmPassword',
            error: undefined,
          });
        }}
        onSave={handleSavePassword}
        t={t}
      />

      {form.apiError && (
        <Text style={commonStyles.apiError}>{form.apiError}</Text>
      )}
    </ScrollView>
  );
};

export default EditProfileScreen;
