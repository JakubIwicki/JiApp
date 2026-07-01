import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { ActivityIndicator, StyleSheet, View } from 'react-native';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useNavigation } from '@react-navigation/native';
import useAuth from '../hooks/useAuth';
import * as storageService from '../services/storageService';
import ModuleSelectionScreen from '../screens/ModuleSelectionScreen';
import MainNavigator from './MainNavigator';
import SchedulerNavigator from './SchedulerNavigator';
import LovingBoardsNavigator from './LovingBoardsNavigator';
import AdminNavigator from './AdminNavigator';
import type { Theme } from '../styles/theme';
import { useThemedStyles, useTheme } from '../context/ThemeContext';
import type { ModuleId, RootStackParamList } from './types';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';

const Stack = createNativeStackNavigator<RootStackParamList>();

type RootNavigationProp = NativeStackNavigationProp<RootStackParamList>;

/** Maps a module id to its route name on the root stack. */
const MODULE_ROUTE: Record<ModuleId, keyof RootStackParamList> = {
  YtDownloader: 'YtDownloader',
  Scheduler: 'Scheduler',
  LovingBoards: 'LovingBoards',
};

/**
 * Decides where a freshly-authenticated user should land:
 * - exactly one granted module → that module (picker skipped);
 * - a persisted, still-granted choice → that module directly;
 * - otherwise → the module picker.
 */
function resolveInitialRoute(
  availableModules: ModuleId[],
  persisted: ModuleId | null,
): keyof RootStackParamList {
  if (availableModules.length === 1) {
    return MODULE_ROUTE[availableModules[0]];
  }
  if (persisted && availableModules.includes(persisted)) {
    return MODULE_ROUTE[persisted];
  }
  return 'ModuleSelection';
}

const ModuleSelectionRoute: React.FC = () => {
  const navigation = useNavigation<RootNavigationProp>();

  const handleSelectModule = useCallback(
    async (moduleId: ModuleId) => {
      await storageService.saveSelectedModule(moduleId);
      navigation.navigate(MODULE_ROUTE[moduleId]);
    },
    [navigation],
  );

  const handleSelectAdmin = useCallback(() => {
    navigation.navigate('Admin');
  }, [navigation]);

  return (
    <ModuleSelectionScreen
      onSelectModule={handleSelectModule}
      onSelectAdmin={handleSelectAdmin}
    />
  );
};

const RootNavigator: React.FC = () => {
  const { availableModules } = useAuth();
  const [persisted, setPersisted] = useState<ModuleId | null>(null);
  const [resolved, setResolved] = useState(false);
  const styles = useThemedStyles(makeStyles);
  const { colors } = useTheme();

  useEffect(() => {
    let active = true;
    storageService
      .getSelectedModule()
      .then(value => {
        if (active) setPersisted(value);
      })
      .finally(() => {
        if (active) setResolved(true);
      });
    return () => {
      active = false;
    };
  }, []); // mount only

  const initialRouteName = useMemo(
    () => resolveInitialRoute(availableModules, persisted),
    [availableModules, persisted],
  );

  if (!resolved) {
    return (
      <View style={styles.loading} testID="root-resolving">
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  return (
    <Stack.Navigator
      initialRouteName={initialRouteName}
      screenOptions={{ headerShown: false }}
    >
      <Stack.Screen name="ModuleSelection" component={ModuleSelectionRoute} />
      <Stack.Screen name="YtDownloader" component={MainNavigator} />
      <Stack.Screen name="Scheduler" component={SchedulerNavigator} />
      <Stack.Screen name="LovingBoards" component={LovingBoardsNavigator} />
      <Stack.Screen name="Admin" component={AdminNavigator} />
    </Stack.Navigator>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    loading: {
      flex: 1,
      alignItems: 'center',
      justifyContent: 'center',
      backgroundColor: t.colors.background,
    },
  });

export default RootNavigator;
