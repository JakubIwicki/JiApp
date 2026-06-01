export interface JiModule {
  id: string;
  name: string; // i18n key
  icon: string; // IconName for TabIcon
  component: React.ComponentType<any>;
  enabled: boolean;
}

export type ShellStackParamList = {
  ModuleLoader: undefined;
  Settings: undefined;
};
