import type { NavigatorScreenParams } from '@react-navigation/native';
import type { VideoItem } from '../types/api';

/** Canonical module identifiers — must match the backend exactly. */
export type ModuleId = 'YtDownloader' | 'Scheduler' | 'LovingBoards';

export type RootStackParamList = {
  ModuleSelection: undefined;
  YtDownloader: undefined;
  Scheduler: undefined;
  LovingBoards: undefined;
  Admin: undefined;
};

export type LovingBoardsStackParamList = {
  BoardList: undefined;
  BoardDetail: { boardId: number };
  BoardMembers: { boardId: number };
  ItemSheet: { boardId: number; itemId?: number };
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
  EditProfile: undefined;
};

export type ChatStackParamList = {
  Chat: undefined;
  Download: VideoItem;
};

export type AdminStackParamList = {
  UserList: undefined;
  UserDetail: { userId: number };
  CreateUser: undefined;
  RoleList: undefined;
  RoleEdit: { roleName: string };
};

export type MainTabParamList = {
  SearchTab: NavigatorScreenParams<MainStackParamList> | undefined;
  AssistantTab: NavigatorScreenParams<ChatStackParamList> | undefined;
  DownloadsTab: undefined;
  HistoryTab: NavigatorScreenParams<HistoryStackParamList> | undefined;
  SettingsTab: NavigatorScreenParams<SettingsStackParamList> | undefined;
};
