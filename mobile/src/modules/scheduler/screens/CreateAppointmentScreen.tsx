import React, { useCallback } from 'react';
import {
  View,
  Text,
  TextInput,
  Pressable,
  ScrollView,
  FlatList,
  StyleSheet,
} from 'react-native';
import { useRoute, RouteProp } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import ClientPicker from '../components/ClientPicker';
import useCreateAppointment from '../hooks/useCreateAppointment';
import { useTheme, useThemedStyles } from '../../../context/ThemeContext';
import type { Theme } from '../../../styles/theme';
import { spacing, borderRadius } from '../../../styles/theme';
import type { ServiceItem } from '../types/api';
import type { SchedulerStackParamList } from '../types/navigation';

type CreateRoute = RouteProp<SchedulerStackParamList, 'CreateAppointment'>;

const SERVICE_CATEGORIES = [
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

const ServiceItemRow = React.memo<{
  service: ServiceItem;
  isSelected: boolean;
  startTime: string;
  onSelect: (serviceId: number, endTime: string) => void;
}>(({ service, isSelected, startTime, onSelect }) => {
  const { t } = useTranslation();
  const styles = useThemedStyles(makeStyles);
  const endTime = calculateEndTime(startTime, service.baseDuration);
  return (
    <Pressable
      style={({ pressed }) => [
        styles.serviceItem,
        isSelected && styles.serviceItemActive,
        pressed && { opacity: 0.7 },
      ]}
      onPress={() => onSelect(service.id, endTime)}
    >
      <View>
        <Text style={styles.serviceName}>{service.name}</Text>
        <Text style={styles.serviceDetail}>
          {t('scheduler.serviceList.durationAndPrice', {
            duration: service.baseDuration,
            amount: service.basePrice.amount,
            currency: service.basePrice.currency,
          })}
        </Text>
      </View>
    </Pressable>
  );
});

const CreateAppointmentScreen: React.FC = () => {
  const route = useRoute<CreateRoute>();
  const { boardId } = route.params;
  const { t } = useTranslation();

  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const {
    form,
    services,
    clients,
    clientsLoading,
    setClient,
    setCategory,
    setDate,
    setStartTime,
    setEndTime,
    setDescription,
    setLocation,
    handleServiceSelect,
    serviceKeyExtractor,
    handleSubmit,
    handleCreateClient,
  } = useCreateAppointment(boardId);

  const serviceRenderItem = useCallback(
    ({ item }: { item: ServiceItem }) => (
      <ServiceItemRow
        service={item}
        isSelected={item.id === form.selectedServiceId}
        startTime={form.startTime}
        onSelect={handleServiceSelect}
      />
    ),
    [form.selectedServiceId, form.startTime, handleServiceSelect],
  );

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.sectionTitle}>
        {t('scheduler.createAppointment.title')}
      </Text>

      {/* Date */}
      <Text style={styles.label}>{t('scheduler.createAppointment.date')}</Text>
      <TextInput
        style={styles.input}
        value={form.date}
        onChangeText={setDate}
        placeholder="YYYY-MM-DD"
        placeholderTextColor={colors.textTertiary}
      />

      {/* Client Picker */}
      <ClientPicker
        clients={clients}
        selectedClientId={form.selectedClientId}
        onSelect={client => setClient(client.id)}
        onCreateNew={handleCreateClient}
        isLoading={clientsLoading}
      />

      {/* Category selector */}
      <Text style={styles.label}>
        {t('scheduler.createAppointment.category')}
      </Text>
      <View style={styles.categoryRow}>
        {SERVICE_CATEGORIES.map(cat => (
          <Pressable
            key={cat}
            style={({ pressed }) => [
              styles.categoryChip,
              form.selectedCategory === cat && styles.categoryChipActive,
              pressed && { opacity: 0.7 },
            ]}
            onPress={() => setCategory(cat)}
          >
            <Text
              style={[
                styles.categoryChipText,
                form.selectedCategory === cat && styles.categoryChipTextActive,
              ]}
            >
              {t(CATEGORY_LABELS[cat])}
            </Text>
          </Pressable>
        ))}
      </View>

      {/* Service picker */}
      <Text style={styles.label}>
        {t('scheduler.createAppointment.service')}
      </Text>
      <FlatList
        data={services}
        scrollEnabled={false}
        keyExtractor={serviceKeyExtractor}
        renderItem={serviceRenderItem}
      />

      {/* Time inputs */}
      <View style={styles.timeRow}>
        <View style={styles.timeField}>
          <Text style={styles.label}>
            {t('scheduler.createAppointment.start')}
          </Text>
          <TextInput
            style={styles.input}
            value={form.startTime}
            onChangeText={setStartTime}
            placeholder="HH:mm"
            placeholderTextColor={colors.textTertiary}
          />
        </View>
        <View style={styles.timeField}>
          <Text style={styles.label}>
            {t('scheduler.createAppointment.end')}
          </Text>
          <TextInput
            style={styles.input}
            value={form.endTime}
            onChangeText={setEndTime}
            placeholder="HH:mm"
            placeholderTextColor={colors.textTertiary}
          />
        </View>
      </View>

      {/* Description */}
      <Text style={styles.label}>
        {t('scheduler.createAppointment.descriptionOptional')}
      </Text>
      <TextInput
        style={[styles.input, styles.textArea]}
        value={form.description}
        onChangeText={setDescription}
        placeholder={t('scheduler.createAppointment.notesPlaceholder')}
        placeholderTextColor={colors.textTertiary}
        multiline
        numberOfLines={3}
      />

      {/* Location */}
      <Text style={styles.label}>
        {t('scheduler.createAppointment.locationOptional')}
      </Text>
      <TextInput
        style={styles.input}
        value={form.location}
        onChangeText={setLocation}
        placeholder={t('scheduler.createAppointment.locationPlaceholder')}
        placeholderTextColor={colors.textTertiary}
      />

      {/* Submit */}
      <Pressable
        style={({ pressed }) => [
          styles.submitButton,
          form.isSubmitting && styles.submitButtonDisabled,
          pressed && { opacity: 0.7 },
        ]}
        onPress={handleSubmit}
        disabled={form.isSubmitting}
      >
        <Text style={styles.submitText}>
          {form.isSubmitting
            ? t('scheduler.createAppointment.creating')
            : t('scheduler.createAppointment.submit')}
        </Text>
      </Pressable>
    </ScrollView>
  );
};

function calculateEndTime(startTime: string, durationMinutes: number): string {
  const [h, m] = startTime.split(':').map(Number);
  const totalMinutes = h * 60 + m + durationMinutes;
  const endH = Math.floor(totalMinutes / 60);
  const endM = totalMinutes % 60;
  return `${String(endH).padStart(2, '0')}:${String(endM).padStart(2, '0')}`;
}

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
    sectionTitle: {
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
    textArea: {
      minHeight: 80,
      textAlignVertical: 'top',
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
    serviceItem: {
      backgroundColor: t.colors.surface,
      borderRadius: borderRadius.md,
      borderWidth: 1,
      borderColor: t.colors.border,
      padding: spacing.md,
      marginBottom: spacing.xs,
    },
    serviceItemActive: {
      borderColor: t.colors.primary,
      backgroundColor: t.colors.primaryLight,
    },
    serviceName: {
      ...t.typography.body,
      color: t.colors.textPrimary,
      fontWeight: '600',
    },
    serviceDetail: {
      ...t.typography.caption,
      color: t.colors.textSecondary,
      marginTop: 2,
    },
    timeRow: {
      flexDirection: 'row',
      gap: spacing.md,
    },
    timeField: {
      flex: 1,
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

export default CreateAppointmentScreen;
