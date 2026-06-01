import React from 'react';
import { View, Text, Pressable, StyleSheet } from 'react-native';
import { createBottomTabNavigator } from '../navigation/bottomTabs';
import { useTranslation } from 'react-i18next';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import type { NativeStackNavigationProp } from '@react-navigation/native-stack';
import TabIcon from '../components/TabIcon';
import TabBarButton from '../components/TabBarButton';
import { colors, tabBar } from '../styles/theme';
import { getEnabledModules } from './ModuleRegistry';
import type { ShellStackParamList } from './types';

type SettingsNavProp = NativeStackNavigationProp<ShellStackParamList, 'ModuleLoader'>;

const Tab = createBottomTabNavigator();

const ModuleLoader: React.FC = () => {
  const { t } = useTranslation();
  const insets = useSafeAreaInsets();
  const navigation = useNavigation<SettingsNavProp>();
  const modules = getEnabledModules();

  if (modules.length === 0) {
    return (
      <View style={styles.emptyContainer}>
        <Text style={styles.emptyText}>{t('shell.modules')}</Text>
      </View>
    );
  }

  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarButton: (props) => <TabBarButton {...props} />,
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
        headerRight: () => (
          <Pressable
            onPress={() => navigation.navigate('Settings')}
            style={({ pressed }) => [styles.settingsButton, pressed && { opacity: 0.7 }]}
            accessibilityRole="button"
          >
            <TabIcon name="settings" color={colors.textPrimary} size={24} />
          </Pressable>
        ),
      }}
    >
      {modules.map((mod) => (
        <Tab.Screen
          key={mod.id}
          name={mod.id}
          component={mod.component}
          options={{
            tabBarLabel: t(mod.name),
            tabBarIcon: ({ color, size }) => (
              <TabIcon name={mod.icon} color={color} size={size} />
            ),
          }}
        />
      ))}
    </Tab.Navigator>
  );
};

const styles = StyleSheet.create({
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 16,
  },
  emptyText: {
    fontSize: 14,
    color: '#999',
    textAlign: 'center',
  },
  settingsButton: {
    marginRight: 16,
  },
});

export default ModuleLoader;
