import React from 'react';
import AuthNavigator from './AuthNavigator';
import MainNavigator from './MainNavigator';

const AppNavigator: React.FC = () => {
  // Hardcoded to AuthNavigator for Phase 0 — will use AuthContext in Phase 1
  const isAuthenticated = false;

  return isAuthenticated ? <MainNavigator /> : <AuthNavigator />;
};

export default AppNavigator;
