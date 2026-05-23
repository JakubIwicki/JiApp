import React from 'react';
import { I18nextProvider } from 'react-i18next';
import type { Preview } from '@storybook/react';
import i18n from '../src/i18n';

const preview: Preview = {
  decorators: [
    (Story: React.FC) => (
      <I18nextProvider i18n={i18n}>
        <Story />
      </I18nextProvider>
    ),
  ],
};

export default preview;
