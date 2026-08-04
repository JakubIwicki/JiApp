import React, { useEffect, useCallback, useReducer, useState } from 'react';
import {
  View,
  Text,
  TextInput,
  Pressable,
  ScrollView,
  Alert,
  StyleSheet,
} from 'react-native';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import * as serviceCatalogService from '../services/serviceCatalogService';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { SchedulerStackParamList } from '../types/navigation';

type EditRoute = RouteProp<SchedulerStackParamList, 'ServiceEdit'>;

const CATEGORIES = [
  'MensHaircut',
  'WomensHaircut',
  'WomensStyling',
  'Coloring',
  'Treatment',
  'Other',
] as const;

const CATEGORY_LABELS: Record<string, string> = {
  MensHaircut: 'scheduler.category.mensHaircut',
  WomensHaircut: 'scheduler.category.womensHaircut',
  WomensStyling: 'scheduler.category.womensStyling',
  Coloring: 'scheduler.category.coloring',
  Treatment: 'scheduler.category.treatment',
  Other: 'scheduler.category.other',
};

interface ServiceFormState {
  name: string;
  category: string;
  duration: string;
  price: string;
}

type ServiceFormAction =
  | { type: 'SET_NAME'; name: string }
  | { type: 'SET_CATEGORY'; category: string }
  | { type: 'SET_DURATION'; duration: string }
  | { type: 'SET_PRICE'; price: string }
  | {
      type: 'LOAD';
      name: string;
      category: string;
      duration: string;
      price: string;
    };

function serviceFormReducer(
  state: ServiceFormState,
  action: ServiceFormAction,
): ServiceFormState {
  switch (action.type) {
    case 'SET_NAME':
      return { ...state, name: action.name };
    case 'SET_CATEGORY':
      return { ...state, category: action.category };
    case 'SET_DURATION':
      return { ...state, duration: action.duration };
    case 'SET_PRICE':
      return { ...state, price: action.price };
    case 'LOAD':
      return {
        name: action.name,
        category: action.category,
        duration: action.duration,
        price: action.price,
      };
    default:
      return state;
  }
}

const initialFormState: ServiceFormState = {
  name: '',
  category: 'MensHaircut',
  duration: '30',
  price: '60',
};

const ServiceEditScreen: React.FC = () => {
  const navigation = useNavigation();
  const route = useRoute<EditRoute>();
  const { serviceId, boardId } = route.params;
  const isEditing = serviceId !== undefined;
  const { t } = useTranslation();

  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const [form, dispatch] = useReducer(serviceFormReducer, initialFormState);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLoading, setIsLoading] = useState(isEditing);

  useEffect(() => {
    if (isEditing && serviceId) {
      serviceCatalogService
        .getService(serviceId)
        .then(svc =>
          dispatch({
            type: 'LOAD',
            name: svc.name,
            category: svc.category,
            duration: String(svc.baseDuration),
            price: String(svc.basePrice.amount),
          }),
        )
        .catch(() =>
          Alert.alert(
            t('scheduler.error'),
            t('scheduler.serviceEdit.loadFailed'),
          ),
        )
        .finally(() => setIsLoading(false));
    }
  }, [isEditing, serviceId, t]);

  const handleSubmit = useCallback(async () => {
    if (!form.name.trim()) {
      Alert.alert(
        t('scheduler.validation'),
        t('scheduler.serviceEdit.nameRequired'),
      );
      return;
    }

    const durationNum = parseInt(form.duration, 10);
    const priceNum = parseFloat(form.price);

    if (isNaN(durationNum) || durationNum <= 0) {
      Alert.alert(
        t('scheduler.validation'),
        t('scheduler.serviceEdit.durationRequired'),
      );
      return;
    }
    if (isNaN(priceNum) || priceNum < 0) {
      Alert.alert(
        t('scheduler.validation'),
        t('scheduler.serviceEdit.priceRequired'),
      );
      return;
    }

    setIsSubmitting(true);
    try {
      if (isEditing && serviceId) {
        await serviceCatalogService.updateService(serviceId, {
          name: form.name.trim(),
          category: form.category,
          baseDuration: durationNum,
          basePrice: { amount: priceNum, currency: 'PLN' },
        });
      } else {
        await serviceCatalogService.createService({
          boardId,
          name: form.name.trim(),
          category: form.category,
          baseDuration: durationNum,
          basePrice: { amount: priceNum, currency: 'PLN' },
        });
      }
      navigation.goBack();
    } catch (err) {
      Alert.alert(
        t('scheduler.error'),
        err instanceof Error
          ? err.message
          : t('scheduler.serviceEdit.saveFailed'),
      );
    } finally {
      setIsSubmitting(false);
    }
  }, [form, isEditing, serviceId, boardId, navigation, t]);

  if (isLoading) {
    return (
      <View style={styles.center}>
        <Text style={styles.loadingText}>{t('common.loading')}</Text>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.title}>
        {isEditing
          ? t('scheduler.serviceEdit.editTitle')
          : t('scheduler.serviceEdit.newTitle')}
      </Text>

      <Text style={styles.label}>{t('scheduler.serviceEdit.name')}</Text>
      <TextInput
        style={styles.input}
        value={form.name}
        onChangeText={name => dispatch({ type: 'SET_NAME', name })}
        placeholder={t('scheduler.serviceEdit.namePlaceholder')}
        placeholderTextColor={colors.textTertiary}
      />

      <Text style={styles.label}>{t('scheduler.serviceEdit.category')}</Text>
      <View style={styles.categoryRow}>
        {CATEGORIES.map(cat => (
          <Pressable
            key={cat}
            style={({ pressed }) => [
              styles.categoryChip,
              form.category === cat && styles.categoryChipActive,
              pressed && { opacity: 0.7 },
            ]}
            onPress={() => dispatch({ type: 'SET_CATEGORY', category: cat })}
          >
            <Text
              style={[
                styles.categoryChipText,
                form.category === cat && styles.categoryChipTextActive,
              ]}
            >
              {t(CATEGORY_LABELS[cat])}
            </Text>
          </Pressable>
        ))}
      </View>

      <Text style={styles.label}>
        {t('scheduler.serviceEdit.durationLabel')}
      </Text>
      <TextInput
        style={styles.input}
        value={form.duration}
        onChangeText={duration => dispatch({ type: 'SET_DURATION', duration })}
        keyboardType="numeric"
        placeholder="30"
        placeholderTextColor={colors.textTertiary}
      />

      <Text style={styles.label}>{t('scheduler.serviceEdit.priceLabel')}</Text>
      <TextInput
        style={styles.input}
        value={form.price}
        onChangeText={price => dispatch({ type: 'SET_PRICE', price })}
        keyboardType="decimal-pad"
        placeholder="60"
        placeholderTextColor={colors.textTertiary}
      />

      <Pressable
        style={({ pressed }) => [
          styles.submitButton,
          isSubmitting && styles.submitButtonDisabled,
          pressed && { opacity: 0.7 },
        ]}
        onPress={handleSubmit}
        disabled={isSubmitting}
      >
        <Text style={styles.submitText}>
          {isSubmitting
            ? t('scheduler.serviceEdit.saving')
            : isEditing
            ? t('scheduler.serviceEdit.updateSubmit')
            : t('scheduler.serviceEdit.createSubmit')}
        </Text>
      </Pressable>
    </ScrollView>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: t.colors.background,
    },
    content: {
      padding: spacing.lg,
      paddingBottom: spacing.xxl,
    },
    center: {
      flex: 1,
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: t.colors.background,
    },
    loadingText: {
      ...t.typography.body,
      color: t.colors.textSecondary,
    },
    title: {
      ...t.typography.heading,
      marginBottom: spacing.lg,
    },
    label: {
      ...t.typography.label,
      color: t.colors.textSecondary,
      marginBottom: spacing.xs,
      marginTop: spacing.md,
    },
    input: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      borderWidth: 1,
      borderColor: t.colors.border,
      paddingHorizontal: spacing.md,
      paddingVertical: 12,
      ...t.typography.body,
      color: t.colors.textPrimary,
    },
    categoryRow: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: spacing.xs,
    },
    categoryChip: {
      borderRadius: borderRadius.xl,
      borderWidth: 1,
      borderColor: t.colors.border,
      paddingHorizontal: spacing.md,
      paddingVertical: spacing.xs,
      backgroundColor: t.colors.surface,
    },
    categoryChipActive: {
      backgroundColor: t.colors.primary,
      borderColor: t.colors.primary,
    },
    categoryChipText: {
      ...t.typography.caption,
      color: t.colors.textSecondary,
    },
    categoryChipTextActive: {
      color: t.colors.textInverse,
    },
    submitButton: {
      backgroundColor: t.colors.primary,
      borderRadius: borderRadius.lg,
      paddingVertical: 14,
      alignItems: 'center',
      marginTop: spacing.xl,
    },
    submitButtonDisabled: {
      opacity: 0.6,
    },
    submitText: {
      ...t.typography.body,
      color: t.colors.textInverse,
      fontWeight: '700',
    },
  });

export default ServiceEditScreen;
