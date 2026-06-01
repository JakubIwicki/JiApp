export interface JiModule {
  id: string;
  name: string; // i18n key
  icon: 'search' | 'downloads' | 'history' | 'settings';
  component: React.ComponentType<any>;
  enabled: boolean;
}

export type ShellStackParamList = {
  ModuleLoader: undefined;
  Settings: undefined;
};
