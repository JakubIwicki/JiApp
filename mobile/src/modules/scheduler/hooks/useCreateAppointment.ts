import { useCallback, useEffect, useReducer, useState } from 'react';
import { Alert } from 'react-native';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import useAppointments from './useAppointments';
import useClients from './useClients';
import * as serviceCatalogService from '../services/serviceCatalogService';
import type { Client, ServiceItem } from '../types/api';
import type { SchedulerStackParamList } from '../types/navigation';

type NavigationProp = NativeStackNavigationProp<
  SchedulerStackParamList,
  'CreateAppointment'
>;

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

export interface UseCreateAppointmentResult {
  form: AppointmentFormState;
  services: ServiceItem[];
  selectedService: ServiceItem | undefined;
  clients: Client[];
  clientsLoading: boolean;
  setClient: (clientId: number | undefined) => void;
  setCategory: (category: string) => void;
  setDate: (date: string) => void;
  setStartTime: (time: string) => void;
  setEndTime: (time: string) => void;
  setDescription: (text: string) => void;
  setLocation: (text: string) => void;
  handleServiceSelect: (serviceId: number, endTime: string) => void;
  serviceKeyExtractor: (item: ServiceItem) => string;
  handleSubmit: () => Promise<void>;
  handleCreateClient: (name: string) => Promise<number | undefined>;
}

const useCreateAppointment = (boardId: number): UseCreateAppointmentResult => {
  const navigation = useNavigation<NavigationProp>();
  const { t } = useTranslation();

  const appointments = useAppointments();
  const clients = useClients(boardId);
  const { loadAll } = clients;

  const [form, dispatch] = useReducer(
    appointmentFormReducer,
    initialAppointmentFormState,
  );
  const [services, setServices] = useState<ServiceItem[]>([]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  useEffect(() => {
    serviceCatalogService
      .listServices(boardId, form.selectedCategory)
      .then(setServices);
  }, [boardId, form.selectedCategory]);

  const selectedService = services.find(s => s.id === form.selectedServiceId);

  const setClient = useCallback((clientId: number | undefined) => {
    dispatch({ type: 'SET_CLIENT', clientId });
  }, []);

  const setCategory = useCallback((category: string) => {
    dispatch({ type: 'SET_CATEGORY', category });
  }, []);

  const setDate = useCallback((date: string) => {
    dispatch({ type: 'SET_DATE', date });
  }, []);

  const setStartTime = useCallback((time: string) => {
    dispatch({ type: 'SET_START_TIME', time });
  }, []);

  const setEndTime = useCallback((time: string) => {
    dispatch({ type: 'SET_END_TIME', time });
  }, []);

  const setDescription = useCallback((text: string) => {
    dispatch({ type: 'SET_DESCRIPTION', text });
  }, []);

  const setLocation = useCallback((text: string) => {
    dispatch({ type: 'SET_LOCATION', text });
  }, []);

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

  const handleSubmit = useCallback(async () => {
    if (!form.selectedClientId || !form.selectedServiceId) {
      Alert.alert(
        t('scheduler.validation'),
        t('scheduler.createAppointment.validationSelect'),
      );
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
        t('scheduler.error'),
        err instanceof Error
          ? err.message
          : t('scheduler.createAppointment.createFailed'),
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
    t,
  ]);

  const handleCreateClient = useCallback(
    async (name: string): Promise<number | undefined> => {
      const result = await clients.addClient({ name });
      return result;
    },
    [clients],
  );

  return {
    form,
    services,
    selectedService,
    clients: clients.clients,
    clientsLoading: clients.isLoading,
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
  };
};

export default useCreateAppointment;
