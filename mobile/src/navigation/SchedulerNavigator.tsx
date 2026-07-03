import React, { useCallback } from 'react';
import { Pressable, StyleSheet, Text } from 'react-native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';
import { BoardProvider } from '../context/BoardContext';
import WeekendGridScreen from '../modules/scheduler/screens/WeekendGridScreen';
import CreateAppointmentScreen from '../modules/scheduler/screens/CreateAppointmentScreen';
import AppointmentDetailScreen from '../modules/scheduler/screens/AppointmentDetailScreen';
import ClientListScreen from '../modules/scheduler/screens/ClientListScreen';
import ClientDetailScreen from '../modules/scheduler/screens/ClientDetailScreen';
import ServiceListScreen from '../modules/scheduler/screens/ServiceListScreen';
import ServiceEditScreen from '../modules/scheduler/screens/ServiceEditScreen';
import ReportsScreen from '../modules/scheduler/screens/ReportsScreen';
import BoardManagementScreen from '../modules/scheduler/screens/BoardManagementScreen';
import { spacing } from '../styles/theme';
import type { Theme } from '../styles/theme';
import { useThemedStyles, useTheme } from '../context/ThemeContext';
import {
  SwitchModuleButton,
  SettingsButton,
} from './components/HeaderNavButtons';
import type { SchedulerStackParamList } from '../modules/scheduler/types/navigation';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

const Stack = createNativeStackNavigator<SchedulerStackParamList>();

const BoardsButton: React.FC = () => {
  const { t } = useTranslation();
  const navigation =
    useNavigation<
      NativeStackNavigationProp<SchedulerStackParamList, 'WeekendGrid'>
    >();
  const styles = useThemedStyles(makeStyles);

  const handlePress = useCallback(() => {
    navigation.navigate('BoardManagement');
  }, [navigation]);

  return (
    <Pressable
      onPress={handlePress}
      style={({ pressed }) => [styles.headerBtn, pressed && styles.pressed]}
      accessibilityRole="button"
      accessibilityLabel={t('boardManagement.title')}
      testID="scheduler-open-boards"
    >
      <Text style={styles.headerBtnText}>{t('boardManagement.title')}</Text>
    </Pressable>
  );
};

const renderHeaderLeft = () => <SwitchModuleButton />;
const renderHeaderRight = () => (
  <>
    <SettingsButton />
    <BoardsButton />
  </>
);

const SchedulerNavigator: React.FC = () => {
  const { t } = useTranslation();
  const { colors } = useTheme();

  const screenOptions = {
    headerStyle: {
      backgroundColor: colors.background,
    },
    headerTintColor: colors.textPrimary,
    headerTitleStyle: {
      fontWeight: '600' as const,
      fontSize: 17,
    },
  };

  return (
    <BoardProvider>
      <Stack.Navigator screenOptions={screenOptions}>
        <Stack.Screen
          name="WeekendGrid"
          component={WeekendGridScreen}
          options={{
            title: t('modules.scheduler.name'),
            headerLeft: renderHeaderLeft,
            headerRight: renderHeaderRight,
          }}
        />
        <Stack.Screen
          name="CreateAppointment"
          component={CreateAppointmentScreen}
        />
        <Stack.Screen
          name="AppointmentDetail"
          component={AppointmentDetailScreen}
        />
        <Stack.Screen name="ClientList" component={ClientListScreen} />
        <Stack.Screen name="ClientDetail" component={ClientDetailScreen} />
        <Stack.Screen name="ServiceList" component={ServiceListScreen} />
        <Stack.Screen name="ServiceEdit" component={ServiceEditScreen} />
        <Stack.Screen name="Reports" component={ReportsScreen} />
        <Stack.Screen
          name="BoardManagement"
          component={BoardManagementScreen}
          options={{ title: t('boardManagement.title') }}
        />
      </Stack.Navigator>
    </BoardProvider>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    headerBtn: {
      minHeight: 44,
      minWidth: 44,
      paddingHorizontal: spacing.sm,
      justifyContent: 'center',
    },
    headerBtnText: {
      ...t.typography.link,
      color: t.colors.primary,
      fontWeight: '600',
    },
    pressed: {
      opacity: 0.6,
    },
  });

export default SchedulerNavigator;
