import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import UserListScreen from '../modules/admin/screens/UserListScreen';
import UserDetailScreen from '../modules/admin/screens/UserDetailScreen';
import CreateUserScreen from '../modules/admin/screens/CreateUserScreen';
import RoleListScreen from '../modules/admin/screens/RoleListScreen';
import RoleEditScreen from '../modules/admin/screens/RoleEditScreen';
import { useTheme } from '../context/ThemeContext';
import {
  SwitchModuleButton,
  SettingsButton,
} from './components/HeaderNavButtons';
import type { AdminStackParamList } from './types';

const Stack = createNativeStackNavigator<AdminStackParamList>();

const renderHeaderLeft = () => <SwitchModuleButton />;
const renderHeaderRight = () => <SettingsButton />;

const AdminNavigator: React.FC = () => {
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
    <Stack.Navigator screenOptions={screenOptions}>
      <Stack.Screen
        name="UserList"
        component={UserListScreen}
        options={{
          title: t('admin.userList.title'),
          headerLeft: renderHeaderLeft,
          headerRight: renderHeaderRight,
        }}
      />
      <Stack.Screen
        name="UserDetail"
        component={UserDetailScreen}
        options={{
          title: t('admin.userDetail.title'),
        }}
      />
      <Stack.Screen
        name="CreateUser"
        component={CreateUserScreen}
        options={{
          title: t('admin.createUser.title'),
        }}
      />
      <Stack.Screen
        name="RoleList"
        component={RoleListScreen}
        options={{
          title: t('admin.roleList.title'),
        }}
      />
      <Stack.Screen
        name="RoleEdit"
        component={RoleEditScreen}
        options={({ route }) => ({
          title: route.params.roleName,
        })}
      />
    </Stack.Navigator>
  );
};

export default AdminNavigator;
