import React, { use, useState, useEffect, useCallback } from 'react';
import { View, ActivityIndicator, BackHandler, StyleSheet } from 'react-native';
import { colors } from '../styles/theme';
import { AuthProvider, AuthContext } from '../context/AuthContext';
import { ToastProvider } from '../context/ToastContext';
import ToastContainer from '../components/ToastContainer';
import WelcomeOverlay from '../components/WelcomeOverlay';
import ConnectionFailureOverlay from '../components/ConnectionFailureOverlay';
import ServerWakeScreen from '../screens/ServerWakeScreen';
import AuthNavigator from './AuthNavigator';
import RootNavigator from './RootNavigator';

const CONNECTION_WATCHDOG_TIMEOUT = 5000;

const AppContent: React.FC = () => {
  const {
    isLoading,
    token,
    showWelcome,
    showFarewell,
    displayName,
    dismissWelcome,
    dismissFarewell,
  } = use(AuthContext);

  const [showWakeScreen, setShowWakeScreen] = useState(() => !__DEV__);
  const [connectionFailed, setConnectionFailed] = useState(false);

  // Connection watchdog: only if the app is STILL loading 5s after the wake
  // screen dismisses do we treat the server as unreachable.
  useEffect(() => {
    if (showWakeScreen || !isLoading) return;
    const timer = setTimeout(() => {
      setConnectionFailed(true);
    }, CONNECTION_WATCHDOG_TIMEOUT);
    return () => clearTimeout(timer);
  }, [showWakeScreen, isLoading]);

  const handleWakeComplete = useCallback(() => {
    setShowWakeScreen(false);
  }, []);

  const handleConnectionTimeout = useCallback(() => {
    BackHandler.exitApp();
  }, []);

  // Show the server wake screen in production builds before anything else
  if (showWakeScreen) {
    return <ServerWakeScreen onComplete={handleWakeComplete} />;
  }

  if (connectionFailed) {
    return (
      <>
        {isLoading && (
          <View style={styles.loadingContainer} testID="loading-screen">
            <ActivityIndicator size="large" color={colors.primary} />
          </View>
        )}
        <ConnectionFailureOverlay
          visible={connectionFailed}
          onTimeout={handleConnectionTimeout}
        />
      </>
    );
  }

  if (isLoading) {
    return (
      <View style={styles.loadingContainer} testID="loading-screen">
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  return (
    <>
      {token ? <RootNavigator /> : <AuthNavigator />}
      {showWelcome && (
        <WelcomeOverlay
          type="welcome"
          displayName={displayName}
          onComplete={dismissWelcome}
        />
      )}
      {showFarewell && (
        <WelcomeOverlay
          type="farewell"
          displayName={null}
          onComplete={dismissFarewell}
        />
      )}
    </>
  );
};

const AppNavigator: React.FC = () => {
  return (
    <AuthProvider>
      <ToastProvider>
        <AppContent />
        <ToastContainer />
      </ToastProvider>
    </AuthProvider>
  );
};

const styles = StyleSheet.create({
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.background,
  },
});

export default AppNavigator;
