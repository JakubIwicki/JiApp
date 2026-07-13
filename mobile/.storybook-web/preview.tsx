import React from 'react';
import { I18nextProvider } from 'react-i18next';
import type { Preview } from '@storybook/react';
import i18n from '../src/i18n';
import { NavigationContainer } from '@react-navigation/native';

const preview: Preview = {
  decorators: [
    (Story: React.FC) => (
      <I18nextProvider i18n={i18n}>
        <NavigationContainer>
          <Story />
        </NavigationContainer>
      </I18nextProvider>
    ),
  ],
};

export default preview;
