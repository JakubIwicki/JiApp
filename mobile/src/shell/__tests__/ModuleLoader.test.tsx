import React from 'react';
import { NavigationContainer } from '@react-navigation/native';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { render, screen } from '@testing-library/react-native';
import { moduleRegistry, registerModule } from '../ModuleRegistry';
import ModuleLoader from '../ModuleLoader';

// Mock TabIcon and TabBarButton to avoid navigation context issues
jest.mock('../../components/TabIcon', () => {
  const { Text } = require('react-native');
  return ({ name }: { name: string }) => <Text>icon-{name}</Text>;
});

jest.mock('../../components/TabBarButton', () => {
  const { TouchableOpacity } = require('react-native');
  return (props: any) => <TouchableOpacity {...props} />;
});

jest.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

const testMetrics = {
  insets: { top: 0, bottom: 0, left: 0, right: 0 },
  frame: { x: 0, y: 0, width: 390, height: 844 },
};

const wrapper = (children: React.ReactNode) => (
  <SafeAreaProvider initialMetrics={testMetrics}>
    <NavigationContainer>{children}</NavigationContainer>
  </SafeAreaProvider>
);

describe('ModuleLoader', () => {
  beforeEach(() => {
    while (moduleRegistry.length > 0) moduleRegistry.pop();
    registerModule({
      id: 'yt-downloader',
      name: 'modules.ytdownloader',
      icon: 'search',
      component: () => null,
      enabled: true,
    });
  });

  it('renders without crashing when modules are registered', () => {
    const { unmount } = render(wrapper(<ModuleLoader />));
    expect(unmount).toBeDefined(); // just verifying it mounts
    unmount();
  });

  it('shows empty state when no modules are enabled', () => {
    while (moduleRegistry.length > 0) moduleRegistry.pop();
    render(wrapper(<ModuleLoader />));
    expect(screen.getByText('shell.modules')).toBeDefined();
  });
});
