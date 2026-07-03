import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useTranslation } from 'react-i18next';
import BoardListScreen from '../modules/lovingBoards/screens/BoardListScreen';
import BoardDetailScreen from '../modules/lovingBoards/screens/BoardDetailScreen';
import BoardMembersScreen from '../modules/lovingBoards/screens/BoardMembersScreen';
import ItemSheet from '../modules/lovingBoards/screens/ItemSheet';
import { useTheme } from '../context/ThemeContext';
import {
  SwitchModuleButton,
  SettingsButton,
} from './components/HeaderNavButtons';
import type { LovingBoardsStackParamList } from './types';

const Stack = createNativeStackNavigator<LovingBoardsStackParamList>();

const renderHeaderLeft = () => <SwitchModuleButton />;
const renderHeaderRight = () => <SettingsButton />;

const LovingBoardsNavigator: React.FC = () => {
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
        name="BoardList"
        component={BoardListScreen}
        options={{
          title: t('lovingBoards.boardList.title'),
          headerLeft: renderHeaderLeft,
          headerRight: renderHeaderRight,
        }}
      />
      <Stack.Screen
        name="BoardDetail"
        component={BoardDetailScreen}
        options={({ route }) => ({
          title: '',
          headerBackTitle: t('lovingBoards.boardList.title'),
        })}
      />
      <Stack.Screen
        name="BoardMembers"
        component={BoardMembersScreen}
        options={{
          title: t('lovingBoards.boardMembers.title'),
        }}
      />
      <Stack.Screen
        name="ItemSheet"
        component={ItemSheet}
        options={{
          presentation: 'modal',
          headerShown: false,
        }}
      />
    </Stack.Navigator>
  );
};

export default LovingBoardsNavigator;
