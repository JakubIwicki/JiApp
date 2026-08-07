import React, { use, useState, useEffect, useCallback } from 'react';
import { View, ActivityIndicator, BackHandler, StyleSheet } from 'react-native';
import type { Theme } from '../styles/theme';
import { useThemedStyles, useTheme } from '../context/ThemeContext';
import { AuthProvider, AuthContext } from '../context/AuthContext';
import { ToastProvider } from '../context/ToastContext';
import ToastContainer from '../components/ToastContainer';
import WelcomeOverlay from '../components/WelcomeOverlay';
import ConnectionFailureOverlay from '../components/ConnectionFailureOverlay';
import ServerWakeScreen from '../screens/ServerWakeScreen';
import UpdateRequiredScreen from '../components/UpdateRequiredScreen';
import { APP_VERSION_CODE } from '../config';
import { fetchAppVersionInfo } from '../services/appVersionService';
import { isUpdateRequired } from '../utils/version';
import type { AppVersionInfo } from '../types/api';
import AuthNavigator from './AuthNavigator';
import RootNavigator from './RootNavigator';

const CONNECTION_WATCHDOG_TIMEOUT = 5000;
const VERSION_CHECK_TIMEOUT = 8000;

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
  const styles = useThemedStyles(makeStyles);
  const { colors } = useTheme();

  const [showWakeScreen, setShowWakeScreen] = useState(() => !__DEV__);
  const [connectionFailed, setConnectionFailed] = useState(false);
  const [updateInfo, setUpdateInfo] = useState<AppVersionInfo | null>(null);
  const [versionChecked, setVersionChecked] = useState(() => __DEV__);

  // Version gate probe — runs once the wake screen completes (server known
  // reachable). Fail-open: any probe error just marks the check done.
  useEffect(() => {
    if (showWakeScreen || __DEV__ || versionChecked) return;

    const controller = new AbortController();
    const timeoutId = setTimeout(
      () => controller.abort(),
      VERSION_CHECK_TIMEOUT,
    );
    let active = true;

    fetchAppVersionInfo(controller.signal)
      .then(info => {
        if (!active) return;
        setUpdateInfo(info);
        setVersionChecked(true);
      })
      .catch(() => {
        if (!active) return;
        setVersionChecked(true);
      })
      .finally(() => clearTimeout(timeoutId));

    return () => {
      active = false;
      clearTimeout(timeoutId);
      controller.abort();
    };
  }, [showWakeScreen, versionChecked]);

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

  const updateRequired =
    updateInfo !== null &&
    isUpdateRequired(APP_VERSION_CODE, updateInfo.minVersionCode);

  // Show the server wake screen in production builds before anything else
  if (showWakeScreen) {
    return <ServerWakeScreen onComplete={handleWakeComplete} />;
  }

  // Hold boot while the version probe is in flight (prod only)
  if (!__DEV__ && !versionChecked) {
    return (
      <View style={styles.loadingContainer} testID="loading-screen">
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  if (updateRequired && updateInfo) {
    return <UpdateRequiredScreen downloadUrl={updateInfo.downloadUrl} />;
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

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    loadingContainer: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      backgroundColor: t.colors.background,
    },
  });

export default AppNavigator;
