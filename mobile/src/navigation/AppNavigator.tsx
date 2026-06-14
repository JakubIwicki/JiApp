import React, { use, useState, useEffect, useRef, useCallback } from 'react';
import { View, ActivityIndicator, BackHandler, StyleSheet } from 'react-native';
import { colors } from '../styles/theme';
import { AuthProvider, AuthContext } from '../context/AuthContext';
import { ToastProvider } from '../context/ToastContext';
import ToastContainer from '../components/ToastContainer';
import WelcomeOverlay from '../components/WelcomeOverlay';
import ConnectionFailureOverlay from '../components/ConnectionFailureOverlay';
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

  const [connectionFailed, setConnectionFailed] = useState(false);
  const watchdogTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Connection watchdog: if isLoading stays true for 5s, assume server unreachable
  useEffect(() => {
    const timer = setTimeout(() => {
      setConnectionFailed(true);
    }, CONNECTION_WATCHDOG_TIMEOUT);

    watchdogTimerRef.current = timer;

    return () => {
      clearTimeout(timer);
    };
  }, []);

  // Clear timer if loading completes before timeout
  useEffect(() => {
    if (!isLoading && watchdogTimerRef.current) {
      clearTimeout(watchdogTimerRef.current);
      watchdogTimerRef.current = null;
    }
  }, [isLoading]);

  const handleConnectionTimeout = useCallback(() => {
    BackHandler.exitApp();
  }, []);

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
