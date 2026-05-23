import React from 'react';
import { I18nextProvider } from 'react-i18next';
import { NavigationContainer } from '@react-navigation/native';
import i18n from '../src/i18n';

export const decorators = [
  (Story: React.FC) => (
    <I18nextProvider i18n={i18n}>
      <NavigationContainer>
        <Story />
      </NavigationContainer>
    </I18nextProvider>
  ),
];
