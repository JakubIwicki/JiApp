import type { NavigatorScreenParams } from '@react-navigation/native';
import type { VideoItem } from '../types/api';

export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
};

export type MainStackParamList = {
  Search: undefined;
  Download: VideoItem;
  History: undefined;
  Settings: undefined;
};

export type HistoryStackParamList = {
  History: undefined;
};

export type SettingsStackParamList = {
  Settings: undefined;
};

export type MainTabParamList = {
  SearchTab: NavigatorScreenParams<MainStackParamList> | undefined;
  DownloadsTab: undefined;
  HistoryTab: NavigatorScreenParams<HistoryStackParamList> | undefined;
  SettingsTab: NavigatorScreenParams<SettingsStackParamList> | undefined;
};
