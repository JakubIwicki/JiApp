import React from 'react';
import { ActivityIndicator, Pressable, Text, View } from 'react-native';
import FormInput from '../components/FormInput';
import { colors, commonStyles } from '../styles/theme';
import editProfileStyles from './editProfileStyles';

interface ChangePasswordSectionProps {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
  currentPasswordError: string | undefined;
  newPasswordError: string | undefined;
  confirmPasswordError: string | undefined;
  loading: boolean;
  onCurrentPasswordChange: (text: string) => void;
  onNewPasswordChange: (text: string) => void;
  onConfirmPasswordChange: (text: string) => void;
  onSave: () => void;
  t: (key: string) => string;
}

const ChangePasswordSection: React.FC<ChangePasswordSectionProps> = ({
  currentPassword,
  newPassword,
  confirmPassword,
  currentPasswordError,
  newPasswordError,
  confirmPasswordError,
  loading,
  onCurrentPasswordChange,
  onNewPasswordChange,
  onConfirmPasswordChange,
  onSave,
  t,
}) => {
  return (
    <View style={commonStyles.sectionContainer}>
      <Text style={commonStyles.sectionHeader}>
        {t('settings.passwordSection')}
      </Text>

      <View style={editProfileStyles.sectionBody}>
        <FormInput
          value={currentPassword}
          onChangeText={onCurrentPasswordChange}
          placeholder={t('settings.currentPassword')}
          error={currentPasswordError}
          label={t('settings.currentPassword')}
          secureTextEntry={true}
        />

        <FormInput
          value={newPassword}
          onChangeText={onNewPasswordChange}
          placeholder={t('settings.newPassword')}
          error={newPasswordError}
          label={t('settings.newPassword')}
          secureTextEntry={true}
        />

        <FormInput
          value={confirmPassword}
          onChangeText={onConfirmPasswordChange}
          placeholder={t('settings.confirmPassword')}
          error={confirmPasswordError}
          label={t('settings.confirmPassword')}
          secureTextEntry={true}
        />

        <Pressable
          style={({ pressed }) => [
            editProfileStyles.saveButton,
            loading && editProfileStyles.saveButtonDisabled,
            pressed && { opacity: 0.7 },
          ]}
          onPress={onSave}
          disabled={loading}
          testID="save-password-button"
          accessibilityRole="button"
          accessibilityLabel={t('settings.save')}
        >
          {loading ? (
            <ActivityIndicator
              color={colors.surface}
              testID="save-password-loading"
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

export default ChangePasswordSection;
