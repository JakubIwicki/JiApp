import { useCallback, useEffect, useReducer, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import * as authService from '../services/authService';
import useAuth from './useAuth';
import useToast from './useToast';
import {
  getFriendlyErrorMessage,
  getServerErrorStatus,
  getServerValidationErrors,
} from '../utils/errorUtils';

const PASSWORD_MIN_LENGTH = 8;
const DISPLAY_NAME_MAX_LENGTH = 50;

type ProfileField =
  | 'displayName'
  | 'email'
  | 'currentPassword'
  | 'newPassword'
  | 'confirmPassword';

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
  | { type: 'SET_FIELD'; field: ProfileField; value: string }
  | { type: 'SET_FIELD_ERROR'; field: ProfileField; error: string | undefined }
  | { type: 'SET_API_ERROR'; error: string | undefined }
  | { type: 'SET_PROFILE_LOADING'; loading: boolean }
  | { type: 'SET_PASSWORD_LOADING'; loading: boolean }
  | { type: 'INIT_FIELDS'; displayName: string; email: string }
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

export interface UseEditProfileResult {
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
  setDisplayName: (text: string) => void;
  setEmail: (text: string) => void;
  setCurrentPassword: (text: string) => void;
  setNewPassword: (text: string) => void;
  setConfirmPassword: (text: string) => void;
  handleSaveProfile: () => Promise<void>;
  handleSavePassword: () => Promise<void>;
}

const useEditProfile = (): UseEditProfileResult => {
  const { t } = useTranslation();
  const { updateProfile } = useAuth();
  const { showSuccess } = useToast();

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

  const setField = useCallback((field: ProfileField, value: string) => {
    dispatch({ type: 'SET_FIELD', field, value });
    dispatch({ type: 'SET_FIELD_ERROR', field, error: undefined });
  }, []);

  const setDisplayName = useCallback(
    (text: string) => setField('displayName', text),
    [setField],
  );
  const setEmail = useCallback(
    (text: string) => setField('email', text),
    [setField],
  );
  const setCurrentPassword = useCallback(
    (text: string) => setField('currentPassword', text),
    [setField],
  );
  const setNewPassword = useCallback(
    (text: string) => setField('newPassword', text),
    [setField],
  );
  const setConfirmPassword = useCallback(
    (text: string) => setField('confirmPassword', text),
    [setField],
  );

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
      const status = getServerErrorStatus(err);
      const validationErrors = getServerValidationErrors(err);

      if (status === 400 && validationErrors.length > 0) {
        const fieldErrors = extractFieldErrors(validationErrors);
        for (const [field, error] of Object.entries(fieldErrors)) {
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: field as ProfileField,
            error,
          });
        }
        return;
      }

      if (status === 409) {
        dispatch({
          type: 'SET_FIELD_ERROR',
          field: 'email',
          error: t('settings.emailTaken'),
        });
        return;
      }

      dispatch({
        type: 'SET_API_ERROR',
        error: getFriendlyErrorMessage(err, t('common.error')),
      });
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
      const status = getServerErrorStatus(err);
      const validationErrors = getServerValidationErrors(err);

      if (status === 400 && validationErrors.length > 0) {
        const fieldErrors = extractFieldErrors(validationErrors);
        for (const [field, error] of Object.entries(fieldErrors)) {
          dispatch({
            type: 'SET_FIELD_ERROR',
            field: field as ProfileField,
            error,
          });
        }
        return;
      }

      dispatch({
        type: 'SET_API_ERROR',
        error: getFriendlyErrorMessage(err, t('common.error')),
      });
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

  return {
    displayName: form.displayName,
    email: form.email,
    currentPassword: form.currentPassword,
    newPassword: form.newPassword,
    confirmPassword: form.confirmPassword,
    displayNameError: form.displayNameError,
    emailError: form.emailError,
    currentPasswordError: form.currentPasswordError,
    newPasswordError: form.newPasswordError,
    confirmPasswordError: form.confirmPasswordError,
    apiError: form.apiError,
    profileLoading: form.profileLoading,
    passwordLoading: form.passwordLoading,
    setDisplayName,
    setEmail,
    setCurrentPassword,
    setNewPassword,
    setConfirmPassword,
    handleSaveProfile,
    handleSavePassword,
  };
};

export default useEditProfile;
