import React from 'react';
import { ScrollView, Text } from 'react-native';
import { useTranslation } from 'react-i18next';
import useEditProfile from '../hooks/useEditProfile';
import ProfileSection from './ProfileSection';
import ChangePasswordSection from './ChangePasswordSection';
import useScreenTitle from '../hooks/useScreenTitle';
import { useTheme } from '../context/ThemeContext';

const EditProfileScreen: React.FC = () => {
  const { t } = useTranslation();

  useScreenTitle('settings.editProfile');
  const { commonStyles } = useTheme();

  const {
    displayName,
    email,
    currentPassword,
    newPassword,
    confirmPassword,
    displayNameError,
    emailError,
    currentPasswordError,
    newPasswordError,
    confirmPasswordError,
    apiError,
    profileLoading,
    passwordLoading,
    setDisplayName,
    setEmail,
    setCurrentPassword,
    setNewPassword,
    setConfirmPassword,
    handleSaveProfile,
    handleSavePassword,
  } = useEditProfile();

  return (
    <ScrollView
      style={commonStyles.screenContainer}
      contentContainerStyle={commonStyles.scrollContent}
    >
      <ProfileSection
        displayName={displayName}
        email={email}
        displayNameError={displayNameError}
        emailError={emailError}
        loading={profileLoading}
        onDisplayNameChange={setDisplayName}
        onEmailChange={setEmail}
        onSave={handleSaveProfile}
        t={t}
      />

      <ChangePasswordSection
        currentPassword={currentPassword}
        newPassword={newPassword}
        confirmPassword={confirmPassword}
        currentPasswordError={currentPasswordError}
        newPasswordError={newPasswordError}
        confirmPasswordError={confirmPasswordError}
        loading={passwordLoading}
        onCurrentPasswordChange={setCurrentPassword}
        onNewPasswordChange={setNewPassword}
        onConfirmPasswordChange={setConfirmPassword}
        onSave={handleSavePassword}
        t={t}
      />

      {apiError && <Text style={commonStyles.apiError}>{apiError}</Text>}
    </ScrollView>
  );
};

export default EditProfileScreen;
