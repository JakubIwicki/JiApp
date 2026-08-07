/**
 * Route-name constants for the YtDownloader module's MainNavigator tree.
 *
 * Single typed source of truth so navigate() targets never drift from the
 * registered screen names. Other navigators (Root/Scheduler/LovingBoards/
 * Admin/Auth) still use raw strings and should adopt this pattern next.
 */
export const Routes = {
  tabs: {
    search: 'SearchTab',
    assistant: 'AssistantTab',
    downloads: 'DownloadsTab',
    history: 'HistoryTab',
    settings: 'SettingsTab',
  },
  search: {
    search: 'Search',
    download: 'Download',
  },
  history: {
    history: 'History',
    download: 'Download',
  },
  downloads: {
    downloadsMain: 'DownloadsMain',
  },
  chat: {
    chat: 'Chat',
    download: 'Download',
  },
  settings: {
    settings: 'Settings',
    editProfile: 'EditProfile',
  },
} as const;
