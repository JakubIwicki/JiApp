import type { NavigatorScreenParams } from '@react-navigation/native';
import type { VideoItem } from '../types/api';

/** Canonical module identifiers — must match the backend exactly. */
export type ModuleId = 'YtDownloader' | 'Scheduler';

export type RootStackParamList = {
  ModuleSelection: undefined;
  YtDownloader: undefined;
  Scheduler: undefined;
};

export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
};

export type MainStackParamList = {
  Search: undefined;
  Download: VideoItem;
  History: undefined;
  DownloadsMain: undefined;
  Settings: undefined;
};

export type HistoryStackParamList = {
  History: undefined;
};

export type SettingsStackParamList = {
  Settings: undefined;
};

export type ChatStackParamList = {
  Chat: undefined;
  Download: VideoItem;
};

export type MainTabParamList = {
  SearchTab: NavigatorScreenParams<MainStackParamList> | undefined;
  AssistantTab: NavigatorScreenParams<ChatStackParamList> | undefined;
  DownloadsTab: undefined;
  HistoryTab: NavigatorScreenParams<HistoryStackParamList> | undefined;
  SettingsTab: NavigatorScreenParams<SettingsStackParamList> | undefined;
};
