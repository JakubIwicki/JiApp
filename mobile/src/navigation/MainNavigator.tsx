import React from 'react';
import { createBottomTabNavigator } from '../navigation/bottomTabs';
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
import { colors, tabBar } from '../styles/theme';
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

const ChatStackScreen: React.FC = () => (
  <ChatStack.Navigator screenOptions={stackScreenOptions}>
    <ChatStack.Screen name="Chat" component={ChatScreen} />
    <ChatStack.Screen name="Download" component={DownloadScreen} />
  </ChatStack.Navigator>
);

const DownloadsStackScreen: React.FC = () => (
  <DownloadsStack.Navigator screenOptions={stackScreenOptions}>
    <DownloadsStack.Screen name="DownloadsMain" component={DownloadsScreen} />
  </DownloadsStack.Navigator>
);

const SettingsStackScreen: React.FC = () => (
  <SettingsStack.Navigator screenOptions={stackScreenOptions}>
    <SettingsStack.Screen name="Settings" component={SettingsScreen} />
    <SettingsStack.Screen name="EditProfile" component={EditProfileScreen} />
  </SettingsStack.Navigator>
);

const MainNavigator: React.FC = () => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();

  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarButton: props => <TabBarButton {...props} />,
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
        name="AssistantTab"
        component={ChatStackScreen}
        options={{
          tabBarLabel: t('nav.assistant'),
          tabBarIcon: ({ color, size }) => (
            <TabIcon name="assistant" color={color} size={size} />
          ),
        }}
      />
      <Tab.Screen
        name="DownloadsTab"
        component={DownloadsStackScreen}
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
