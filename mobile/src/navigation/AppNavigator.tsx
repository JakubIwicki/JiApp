import React, { use } from 'react';
import { View, ActivityIndicator, StyleSheet } from 'react-native';
import { colors } from '../styles/theme';
import { AuthProvider, AuthContext } from '../context/AuthContext';
import { ToastProvider } from '../context/ToastContext';
import ToastContainer from '../components/ToastContainer';
import AuthNavigator from './AuthNavigator';
import MainNavigator from './MainNavigator';

const AppContent: React.FC = () => {
  const { isLoading, token } = use(AuthContext);

  if (isLoading) {
    return (
      <View style={styles.loadingContainer} testID="loading-screen">
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  return token ? <MainNavigator /> : <AuthNavigator />;
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
