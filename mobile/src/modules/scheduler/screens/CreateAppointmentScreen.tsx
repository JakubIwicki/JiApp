import React, { useState, useEffect, useCallback, useReducer } from 'react';
import {
  View,
  Text,
  TextInput,
  Pressable,
  ScrollView,
  FlatList,
  StyleSheet,
  Alert,
} from 'react-native';
import { useNavigation, useRoute, RouteProp } from '@react-navigation/native';
import ClientPicker from '../components/ClientPicker';
import useAppointments from '../hooks/useAppointments';
import useClients from '../hooks/useClients';
import * as serviceCatalogService from '../services/serviceCatalogService';
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

const ServiceItemRow = React.memo<{
  service: ServiceItem;
  isSelected: boolean;
  startTime: string;
  onSelect: (serviceId: number, endTime: string) => void;
}>(({ service, isSelected, startTime, onSelect }) => {
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
          {service.baseDuration} min | {service.basePrice.amount}{' '}
          {service.basePrice.currency}
        </Text>
      </View>
    </Pressable>
  );
});

interface AppointmentFormState {
  selectedClientId: number | undefined;
  selectedServiceId: number | undefined;
  selectedCategory: string;
  startTime: string;
  endTime: string;
  description: string;
  location: string;
  date: string;
  isSubmitting: boolean;
}

type AppointmentFormAction =
  | { type: 'SET_CLIENT'; clientId: number | undefined }
  | { type: 'SET_SERVICE'; serviceId: number | undefined; endTime: string }
  | { type: 'SET_CATEGORY'; category: string }
  | { type: 'SET_START_TIME'; time: string }
  | { type: 'SET_END_TIME'; time: string }
  | { type: 'SET_DESCRIPTION'; text: string }
  | { type: 'SET_LOCATION'; text: string }
  | { type: 'SET_DATE'; date: string }
  | { type: 'SET_SUBMITTING'; submitting: boolean };

const initialAppointmentFormState: AppointmentFormState = {
  selectedClientId: undefined,
  selectedServiceId: undefined,
  selectedCategory: 'MensHaircut',
  startTime: '09:00',
  endTime: '10:00',
  description: '',
  location: '',
  date: new Date().toISOString().split('T')[0],
  isSubmitting: false,
};

function appointmentFormReducer(
  state: AppointmentFormState,
  action: AppointmentFormAction,
): AppointmentFormState {
  switch (action.type) {
    case 'SET_CLIENT':
      return { ...state, selectedClientId: action.clientId };
    case 'SET_SERVICE':
      return {
        ...state,
        selectedServiceId: action.serviceId,
        endTime: action.endTime,
      };
    case 'SET_CATEGORY':
      return { ...state, selectedCategory: action.category };
    case 'SET_START_TIME':
      return { ...state, startTime: action.time };
    case 'SET_END_TIME':
      return { ...state, endTime: action.time };
    case 'SET_DESCRIPTION':
      return { ...state, description: action.text };
    case 'SET_LOCATION':
      return { ...state, location: action.text };
    case 'SET_DATE':
      return { ...state, date: action.date };
    case 'SET_SUBMITTING':
      return { ...state, isSubmitting: action.submitting };
    default:
      return state;
  }
}

const CreateAppointmentScreen: React.FC = () => {
  const navigation = useNavigation();
  const route = useRoute<CreateRoute>();
  const { boardId } = route.params;

  const appointments = useAppointments();
  const clients = useClients(boardId);

  const { colors } = useTheme();
  const styles = useThemedStyles(makeStyles);

  const [form, dispatch] = useReducer(
    appointmentFormReducer,
    initialAppointmentFormState,
  );
  const [services, setServices] = useState<ServiceItem[]>([]);

  useEffect(() => {
    clients.loadAll();
  }, [clients.loadAll]);

  useEffect(() => {
    serviceCatalogService
      .listServices(boardId, form.selectedCategory)
      .then(setServices);
  }, [boardId, form.selectedCategory]);

  const selectedService = services.find(s => s.id === form.selectedServiceId);

  const handleServiceSelect = useCallback(
    (serviceId: number, endTime: string) => {
      dispatch({ type: 'SET_SERVICE', serviceId, endTime });
    },
    [],
  );

  const serviceKeyExtractor = useCallback(
    (item: ServiceItem) => String(item.id),
    [],
  );

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

  const handleSubmit = useCallback(async () => {
    if (!form.selectedClientId || !form.selectedServiceId) {
      Alert.alert('Validation', 'Please select client and service');
      return;
    }

    dispatch({ type: 'SET_SUBMITTING', submitting: true });
    try {
      await appointments.addAppointment({
        boardId,
        clientId: form.selectedClientId,
        serviceId: form.selectedServiceId,
        date: form.date,
        startTime: form.startTime,
        endTime: form.endTime,
        description: form.description || undefined,
        location: form.location,
        price: selectedService
          ? {
              amount: selectedService.basePrice.amount,
              currency: selectedService.basePrice.currency,
            }
          : { amount: 0, currency: 'PLN' },
      });
      navigation.goBack();
    } catch (err) {
      Alert.alert(
        'Error',
        err instanceof Error ? err.message : 'Failed to create appointment',
      );
    } finally {
      dispatch({ type: 'SET_SUBMITTING', submitting: false });
    }
  }, [
    form.selectedClientId,
    form.selectedServiceId,
    form.date,
    form.startTime,
    form.endTime,
    form.description,
    form.location,
    boardId,
    selectedService,
    appointments,
    navigation,
  ]);

  const handleCreateClient = useCallback(
    async (name: string): Promise<number | undefined> => {
      const result = await clients.addClient({ name });
      return result;
    },
    [clients],
  );

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.sectionTitle}>New Appointment</Text>

      {/* Date */}
      <Text style={styles.label}>Date</Text>
      <TextInput
        style={styles.input}
        value={form.date}
        onChangeText={date => dispatch({ type: 'SET_DATE', date })}
        placeholder="YYYY-MM-DD"
        placeholderTextColor={colors.textTertiary}
      />

      {/* Client Picker */}
      <ClientPicker
        clients={clients.clients}
        selectedClientId={form.selectedClientId}
        onSelect={client =>
          dispatch({ type: 'SET_CLIENT', clientId: client.id })
        }
        onCreateNew={handleCreateClient}
        isLoading={clients.isLoading}
      />

      {/* Category selector */}
      <Text style={styles.label}>Category</Text>
      <View style={styles.categoryRow}>
        {SERVICE_CATEGORIES.map(cat => (
          <Pressable
            key={cat}
            style={({ pressed }) => [
              styles.categoryChip,
              form.selectedCategory === cat && styles.categoryChipActive,
              pressed && { opacity: 0.7 },
            ]}
            onPress={() => dispatch({ type: 'SET_CATEGORY', category: cat })}
          >
            <Text
              style={[
                styles.categoryChipText,
                form.selectedCategory === cat && styles.categoryChipTextActive,
              ]}
            >
              {cat}
            </Text>
          </Pressable>
        ))}
      </View>

      {/* Service picker */}
      <Text style={styles.label}>Service</Text>
      <FlatList
        data={services}
        scrollEnabled={false}
        keyExtractor={serviceKeyExtractor}
        renderItem={serviceRenderItem}
      />

      {/* Time inputs */}
      <View style={styles.timeRow}>
        <View style={styles.timeField}>
          <Text style={styles.label}>Start</Text>
          <TextInput
            style={styles.input}
            value={form.startTime}
            onChangeText={time => dispatch({ type: 'SET_START_TIME', time })}
            placeholder="HH:mm"
            placeholderTextColor={colors.textTertiary}
          />
        </View>
        <View style={styles.timeField}>
          <Text style={styles.label}>End</Text>
          <TextInput
            style={styles.input}
            value={form.endTime}
            onChangeText={time => dispatch({ type: 'SET_END_TIME', time })}
            placeholder="HH:mm"
            placeholderTextColor={colors.textTertiary}
          />
        </View>
      </View>

      {/* Description */}
      <Text style={styles.label}>Description (optional)</Text>
      <TextInput
        style={[styles.input, styles.textArea]}
        value={form.description}
        onChangeText={text => dispatch({ type: 'SET_DESCRIPTION', text })}
        placeholder="Notes…"
        placeholderTextColor={colors.textTertiary}
        multiline
        numberOfLines={3}
      />

      {/* Location */}
      <Text style={styles.label}>Location (optional)</Text>
      <TextInput
        style={styles.input}
        value={form.location}
        onChangeText={text => dispatch({ type: 'SET_LOCATION', text })}
        placeholder="e.g. Salon"
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
          {form.isSubmitting ? 'Creating…' : 'Create Appointment'}
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
