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
import * as serviceCatalogService from '../services/serviceCatalogService';
import { colors, typography, spacing, borderRadius } from '../../../styles/theme';
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
  | { type: 'LOAD'; name: string; category: string; duration: string; price: string };

function serviceFormReducer(state: ServiceFormState, action: ServiceFormAction): ServiceFormState {
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
      return { name: action.name, category: action.category, duration: action.duration, price: action.price };
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

  const [form, dispatch] = useReducer(serviceFormReducer, initialFormState);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLoading, setIsLoading] = useState(isEditing);

  useEffect(() => {
    if (isEditing && serviceId) {
      serviceCatalogService
        .getService(serviceId)
        .then((svc) =>
          dispatch({
            type: 'LOAD',
            name: svc.name,
            category: svc.category,
            duration: String(svc.baseDuration),
            price: String(svc.basePrice.amount),
          }),
        )
        .catch(() => Alert.alert('Error', 'Failed to load service'))
        .finally(() => setIsLoading(false));
    }
  }, [isEditing, serviceId]);

  const handleSubmit = useCallback(async () => {
    if (!form.name.trim()) {
      Alert.alert('Validation', 'Name is required');
      return;
    }

    const durationNum = parseInt(form.duration, 10);
    const priceNum = parseFloat(form.price);

    if (isNaN(durationNum) || durationNum <= 0) {
      Alert.alert('Validation', 'Valid duration is required');
      return;
    }
    if (isNaN(priceNum) || priceNum < 0) {
      Alert.alert('Validation', 'Valid price is required');
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
      Alert.alert('Error', err instanceof Error ? err.message : 'Failed to save service');
    } finally {
      setIsSubmitting(false);
    }
  }, [form, isEditing, serviceId, boardId, navigation]);

  if (isLoading) {
    return (
      <View style={styles.center}>
        <Text style={styles.loadingText}>Loading…</Text>
      </View>
    );
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.title}>{isEditing ? 'Edit Service' : 'New Service'}</Text>

      <Text style={styles.label}>Name</Text>
      <TextInput
        style={styles.input}
        value={form.name}
        onChangeText={(name) => dispatch({ type: 'SET_NAME', name })}
        placeholder="e.g. Premium Haircut"
        placeholderTextColor={colors.textTertiary}
      />

      <Text style={styles.label}>Category</Text>
      <View style={styles.categoryRow}>
        {CATEGORIES.map((cat) => (
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
              {cat}
            </Text>
          </Pressable>
        ))}
      </View>

      <Text style={styles.label}>Base Duration (minutes)</Text>
      <TextInput
        style={styles.input}
        value={form.duration}
        onChangeText={(duration) => dispatch({ type: 'SET_DURATION', duration })}
        keyboardType="numeric"
        placeholder="30"
        placeholderTextColor={colors.textTertiary}
      />

      <Text style={styles.label}>Base Price (PLN)</Text>
      <TextInput
        style={styles.input}
        value={form.price}
        onChangeText={(price) => dispatch({ type: 'SET_PRICE', price })}
        keyboardType="decimal-pad"
        placeholder="60"
        placeholderTextColor={colors.textTertiary}
      />

      <Pressable
        style={({ pressed }) => [styles.submitButton, isSubmitting && styles.submitButtonDisabled, pressed && { opacity: 0.7 }]}
        onPress={handleSubmit}
        disabled={isSubmitting}
      >
        <Text style={styles.submitText}>
          {isSubmitting ? 'Saving…' : isEditing ? 'Update Service' : 'Create Service'}
        </Text>
      </Pressable>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  content: {
    padding: spacing.lg,
    paddingBottom: spacing.xxl,
  },
  center: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.background,
  },
  loadingText: {
    ...typography.body,
    color: colors.textSecondary,
  },
  title: {
    ...typography.heading,
    marginBottom: spacing.lg,
  },
  label: {
    ...typography.label,
    color: colors.textSecondary,
    marginBottom: spacing.xs,
    marginTop: spacing.md,
  },
  input: {
    backgroundColor: colors.surface,
    borderRadius: borderRadius.md,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingVertical: 12,
    ...typography.body,
    color: colors.textPrimary,
  },
  categoryRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.xs,
  },
  categoryChip: {
    borderRadius: borderRadius.xl,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.xs,
    backgroundColor: colors.surface,
  },
  categoryChipActive: {
    backgroundColor: colors.primary,
    borderColor: colors.primary,
  },
  categoryChipText: {
    ...typography.caption,
    color: colors.textSecondary,
  },
  categoryChipTextActive: {
    color: colors.textInverse,
  },
  submitButton: {
    backgroundColor: colors.primary,
    borderRadius: borderRadius.lg,
    paddingVertical: 14,
    alignItems: 'center',
    marginTop: spacing.xl,
  },
  submitButtonDisabled: {
    opacity: 0.6,
  },
  submitText: {
    ...typography.body,
    color: colors.textInverse,
    fontWeight: '700',
  },
});

export default ServiceEditScreen;
