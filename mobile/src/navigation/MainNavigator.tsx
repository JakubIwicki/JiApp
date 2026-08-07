import React from 'react';
import {
  createBottomTabNavigator,
  type BottomTabBarButtonProps,
} from '../navigation/bottomTabs';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import TabIcon from '../components/TabIcon';
import TabBarButton from '../components/TabBarButton';
import SearchScreen from '../screens/SearchScreen';
import ChatScreen from '../screens/ChatScreen';
import DownloadScreen from '../screens/DownloadScreen';
import DownloadsScreen from '../screens/DownloadsScreen';
import HistoryScreen from '../screens/HistoryScreen';
import SettingsScreen from '../screens/SettingsScreen';
import EditProfileScreen from '../screens/EditProfileScreen';
import { useTheme } from '../context/ThemeContext';
import { Routes } from './routes';
import type {
  MainTabParamList,
  MainStackParamList,
  HistoryStackParamList,
  SettingsStackParamList,
  ChatStackParamList,
} from './types';

const Tab = createBottomTabNavigator<MainTabParamList>();

const SearchStack = createNativeStackNavigator<MainStackParamList>();
const HistoryStack = createNativeStackNavigator<HistoryStackParamList>();
const SettingsStack = createNativeStackNavigator<SettingsStackParamList>();
const DownloadsStack = createNativeStackNavigator<MainStackParamList>();
const ChatStack = createNativeStackNavigator<ChatStackParamList>();

const useStackScreenOptions = () => {
  const { colors } = useTheme();
  return {
    headerStyle: {
      backgroundColor: colors.background,
    },
    headerTintColor: colors.textPrimary,
    headerTitleStyle: {
      fontWeight: '600' as const,
      fontSize: 17,
    },
  };
};

const renderTabBarButton = (props: BottomTabBarButtonProps) => (
  <TabBarButton {...props} />
);

interface TabBarIconProps {
  color: string;
  size: number;
}

const renderSearchTabIcon = ({ color, size }: TabBarIconProps) => (
  <TabIcon name="search" color={color} size={size} />
);
const renderAssistantTabIcon = ({ color, size }: TabBarIconProps) => (
  <TabIcon name="assistant" color={color} size={size} />
);
const renderDownloadsTabIcon = ({ color, size }: TabBarIconProps) => (
  <TabIcon name="downloads" color={color} size={size} />
);
const renderHistoryTabIcon = ({ color, size }: TabBarIconProps) => (
  <TabIcon name="history" color={color} size={size} />
);
const renderSettingsTabIcon = ({ color, size }: TabBarIconProps) => (
  <TabIcon name="settings" color={color} size={size} />
);

const SearchStackScreen: React.FC = () => {
  const screenOptions = useStackScreenOptions();
  return (
    <SearchStack.Navigator screenOptions={screenOptions}>
      <SearchStack.Screen
        name={Routes.search.search}
        component={SearchScreen}
      />
      <SearchStack.Screen
        name={Routes.search.download}
        component={DownloadScreen}
      />
    </SearchStack.Navigator>
  );
};

const HistoryStackScreen: React.FC = () => {
  const screenOptions = useStackScreenOptions();
  return (
    <HistoryStack.Navigator screenOptions={screenOptions}>
      <HistoryStack.Screen
        name={Routes.history.history}
        component={HistoryScreen}
      />
      <HistoryStack.Screen
        name={Routes.history.download}
        component={DownloadScreen}
      />
    </HistoryStack.Navigator>
  );
};

const ChatStackScreen: React.FC = () => {
  const screenOptions = useStackScreenOptions();
  return (
    <ChatStack.Navigator screenOptions={screenOptions}>
      <ChatStack.Screen name={Routes.chat.chat} component={ChatScreen} />
      <ChatStack.Screen
        name={Routes.chat.download}
        component={DownloadScreen}
      />
    </ChatStack.Navigator>
  );
};

const DownloadsStackScreen: React.FC = () => {
  const screenOptions = useStackScreenOptions();
  return (
    <DownloadsStack.Navigator screenOptions={screenOptions}>
      <DownloadsStack.Screen
        name={Routes.downloads.downloadsMain}
        component={DownloadsScreen}
      />
    </DownloadsStack.Navigator>
  );
};

const SettingsStackScreen: React.FC = () => {
  const screenOptions = useStackScreenOptions();
  return (
    <SettingsStack.Navigator screenOptions={screenOptions}>
      <SettingsStack.Screen
        name={Routes.settings.settings}
        component={SettingsScreen}
      />
      <SettingsStack.Screen
        name={Routes.settings.editProfile}
        component={EditProfileScreen}
      />
    </SettingsStack.Navigator>
  );
};

const MainNavigator: React.FC = () => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const { colors, tabBar } = useTheme();

  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarButton: renderTabBarButton,
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
        name={Routes.tabs.search}
        component={SearchStackScreen}
        options={{
          tabBarLabel: t('nav.search'),
          tabBarIcon: renderSearchTabIcon,
        }}
      />
      <Tab.Screen
        name={Routes.tabs.assistant}
        component={ChatStackScreen}
        options={{
          tabBarLabel: t('nav.assistant'),
          tabBarIcon: renderAssistantTabIcon,
        }}
      />
      <Tab.Screen
        name={Routes.tabs.downloads}
        component={DownloadsStackScreen}
        options={{
          tabBarLabel: t('nav.downloads'),
          tabBarIcon: renderDownloadsTabIcon,
        }}
      />
      <Tab.Screen
        name={Routes.tabs.history}
        component={HistoryStackScreen}
        options={{
          tabBarLabel: t('nav.history'),
          tabBarIcon: renderHistoryTabIcon,
        }}
      />
      <Tab.Screen
        name={Routes.tabs.settings}
        component={SettingsStackScreen}
        options={{
          tabBarLabel: t('nav.settings'),
          tabBarIcon: renderSettingsTabIcon,
        }}
      />
    </Tab.Navigator>
  );
};

export default MainNavigator;
