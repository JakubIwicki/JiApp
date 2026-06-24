import React from 'react';
import { ActivityIndicator, Pressable, Text, View } from 'react-native';
import FormInput from '../components/FormInput';
import { colors, commonStyles } from '../styles/theme';
import editProfileStyles from './editProfileStyles';

interface ProfileSectionProps {
  displayName: string;
  email: string;
  displayNameError: string | undefined;
  emailError: string | undefined;
  loading: boolean;
  onDisplayNameChange: (text: string) => void;
  onEmailChange: (text: string) => void;
  onSave: () => void;
  t: (key: string) => string;
}

const ProfileSection: React.FC<ProfileSectionProps> = ({
  displayName,
  email,
  displayNameError,
  emailError,
  loading,
  onDisplayNameChange,
  onEmailChange,
  onSave,
  t,
}) => {
  return (
    <View style={commonStyles.sectionContainer}>
      <Text style={commonStyles.sectionHeader}>
        {t('settings.profileSection')}
      </Text>

      <View style={editProfileStyles.sectionBody}>
        <FormInput
          value={displayName}
          onChangeText={onDisplayNameChange}
          placeholder={t('auth.displayName')}
          error={displayNameError}
          label={t('auth.displayName')}
        />

        <FormInput
          value={email}
          onChangeText={onEmailChange}
          placeholder={t('settings.email')}
          error={emailError}
          label={t('settings.email')}
          keyboardType="email-address"
          autoCapitalize="none"
        />

        <Pressable
          style={({ pressed }) => [
            editProfileStyles.saveButton,
            loading && editProfileStyles.saveButtonDisabled,
            pressed && { opacity: 0.7 },
          ]}
          onPress={onSave}
          disabled={loading}
          testID="save-profile-button"
          accessibilityRole="button"
          accessibilityLabel={t('settings.save')}
        >
          {loading ? (
            <ActivityIndicator
              color={colors.surface}
              testID="save-profile-loading"
              size="small"
            />
          ) : (
            <Text style={editProfileStyles.saveButtonText}>
              {t('settings.save')}
            </Text>
          )}
        </Pressable>
      </View>
    </View>
  );
};

export default ProfileSection;
