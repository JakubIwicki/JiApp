import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { createStackNavigator } from '@react-navigation/stack';
import { useTranslation } from 'react-i18next';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import TabIcon from '../components/TabIcon';
import SearchScreen from '../screens/SearchScreen';
import DownloadScreen from '../screens/DownloadScreen';
import DownloadsScreen from '../screens/DownloadsScreen';
import HistoryScreen from '../screens/HistoryScreen';
import SettingsScreen from '../screens/SettingsScreen';
import {
  colors,
  tabBar,
} from '../styles/theme';
import type {
  MainTabParamList,
  MainStackParamList,
  HistoryStackParamList,
  SettingsStackParamList,
} from './types';

const Tab = createBottomTabNavigator<MainTabParamList>();

const SearchStack = createStackNavigator<MainStackParamList>();
const HistoryStack = createStackNavigator<HistoryStackParamList>();
const SettingsStack = createStackNavigator<SettingsStackParamList>();

const stackScreenOptions = {
  headerStyle: {
    backgroundColor: colors.background,
  },
  headerTintColor: colors.textPrimary,
  headerTitleStyle: {
    fontWeight: '600' as const,
    fontSize: 17,
  },
};

const SearchStackScreen: React.FC = () => (
  <SearchStack.Navigator screenOptions={stackScreenOptions}>
    <SearchStack.Screen name="Search" component={SearchScreen} />
    <SearchStack.Screen name="Download" component={DownloadScreen} />
  </SearchStack.Navigator>
);

const HistoryStackScreen: React.FC = () => (
  <HistoryStack.Navigator screenOptions={stackScreenOptions}>
    <HistoryStack.Screen name="History" component={HistoryScreen} />
  </HistoryStack.Navigator>
);

const SettingsStackScreen: React.FC = () => (
  <SettingsStack.Navigator screenOptions={stackScreenOptions}>
    <SettingsStack.Screen name="Settings" component={SettingsScreen} />
  </SettingsStack.Navigator>
);

const MainNavigator: React.FC = () => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();

  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: tabBar.activeColor,
        tabBarInactiveTintColor: tabBar.inactiveColor,
        tabBarStyle: {
          backgroundColor: colors.surface,
          borderTopColor: colors.separator,
          height: tabBar.height + insets.bottom,
          paddingBottom: insets.bottom,
        },
        tabBarLabelStyle: {
          fontSize: tabBar.labelSize,
          fontWeight: '500',
          marginBottom: 4,
        },
      }}
    >
      <Tab.Screen
        name="SearchTab"
        component={SearchStackScreen}
        options={{
          tabBarLabel: t('nav.search'),
          tabBarIcon: ({ color, size }) => (
            <TabIcon name="search" color={color} size={size} />
          ),
        }}
      />
      <Tab.Screen
        name="DownloadsTab"
        component={DownloadsScreen}
        options={{
          tabBarLabel: t('nav.downloads'),
          tabBarIcon: ({ color, size }) => (
            <TabIcon name="downloads" color={color} size={size} />
          ),
        }}
      />
      <Tab.Screen
        name="HistoryTab"
        component={HistoryStackScreen}
        options={{
          tabBarLabel: t('nav.history'),
          tabBarIcon: ({ color, size }) => (
            <TabIcon name="history" color={color} size={size} />
          ),
        }}
      />
      <Tab.Screen
        name="SettingsTab"
        component={SettingsStackScreen}
        options={{
          tabBarLabel: t('nav.settings'),
          tabBarIcon: ({ color, size }) => (
            <TabIcon name="settings" color={color} size={size} />
          ),
        }}
      />
    </Tab.Navigator>
  );
};

export default MainNavigator;
