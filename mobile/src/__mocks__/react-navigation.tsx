import React from 'react';
import { View, Text, StyleSheet } from 'react-native';

// Mutable state — stories set these before rendering
let _routeParams: Record<string, unknown> = {};
let _screenTitle = '';

export const setRouteParams = (params: Record<string, unknown>) => {
  _routeParams = params;
};
export const setScreenTitle = (title: string) => {
  _screenTitle = title;
};
export const resetNavigationMocks = () => {
  _routeParams = {};
  _screenTitle = '';
};

const navigation = {
  navigate: (..._args: unknown[]) => {},
  setOptions: (_opts: Record<string, unknown>) => {
    if (_opts.title) _screenTitle = _opts.title;
  },
  goBack: () => {},
  addListener: () => () => {},
  removeListener: () => {},
  isFocused: () => true,
};

// @react-navigation/native mocks
export const NavigationContainer: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => <>{children}</>;

export const useNavigation = <T,>() => navigation as T;
export const useRoute = <T,>() =>
  ({ params: _routeParams, name: '', key: '' }) as T;
export const useIsFocused = () => true;
export const CommonActions = { navigate: () => {}, reset: () => {}, goBack: () => {} };
export const StackActions = { push: () => {}, pop: () => {}, replace: () => {} };

// @react-navigation/native-stack mocks
export const createNativeStackNavigator = () => ({
  Navigator: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  Screen: ({
    children,
    component: Component,
    initialParams,
  }: {
    children?: React.ReactNode;
    component?: React.ComponentType<unknown>;
    initialParams?: Record<string, unknown>;
  }) => {
    if (initialParams) _routeParams = initialParams;
    return Component ? <Component /> : <>{children}</>;
  },
  Group: ({ children }: { children: React.ReactNode }) => <>{children}</>,
});

// ─── Bottom Tab Navigator Mock ────────────────────────────────────────────
// Renders a visual tab bar so stories show the full app shell.
// Tabs are purely visual — clicking does not switch screens.

const TAB_LABELS: Record<string, string> = {
  SearchTab: 'Search',
  DownloadsTab: 'Downloads',
  HistoryTab: 'History',
  SettingsTab: 'Settings',
};

const TAB_ICONS: Record<string, string> = {
  SearchTab: '🔍',
  DownloadsTab: '⬇',
  HistoryTab: '🕐',
  SettingsTab: '⚙',
};

export const createBottomTabNavigator = () => ({
  Navigator: ({ children }: { children: React.ReactNode }) => {
    const childrenArr = React.Children.toArray(children) as React.ReactElement[];
    const activeChild = childrenArr.find(
      (c) => c.props?.initialParams?.active === true,
    ) || childrenArr[0];
    const activeName = (activeChild as any)?.props?.name || 'SearchTab';

    return (
      <View style={btStyles.shell}>
        <View style={btStyles.content}>{children}</View>
        <View style={btStyles.tabBar}>
          {childrenArr.map((child: React.ReactElement) => {
            const name = child.props?.name || '';
            const isActive = name === activeName;
            return (
              <View key={name} style={btStyles.tab}>
                <Text style={btStyles.tabIcon}>{TAB_ICONS[name] || '●'}</Text>
                <Text
                  style={[
                    btStyles.tabLabel,
                    { color: isActive ? '#8B7E74' : '#C0B8AE', fontWeight: isActive ? '600' : '400' },
                  ]}
                >
                  {TAB_LABELS[name] || name}
                </Text>
              </View>
            );
          })}
        </View>
      </View>
    );
  },
  Screen: ({
    children,
    component: Component,
    initialParams,
  }: {
    children?: React.ReactNode;
    component?: React.ComponentType<unknown>;
    initialParams?: Record<string, unknown>;
    name?: string;
  }) => {
    if (initialParams) _routeParams = initialParams;
    return Component ? <Component /> : <>{children}</>;
  },
});

const btStyles = StyleSheet.create({
  shell: { flex: 1 },
  content: { flex: 1 },
  tabBar: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    paddingTop: 8,
    paddingBottom: 14,
    borderTopWidth: 1,
    borderTopColor: '#E8E0D8',
    backgroundColor: '#FFFFFF',
  },
  tab: {
    alignItems: 'center',
    gap: 2,
  },
  tabIcon: {
    fontSize: 18,
  },
  tabLabel: {
    fontSize: 9,
  },
});
