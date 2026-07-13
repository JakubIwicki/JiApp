import React from 'react';
import { render, RenderOptions } from '@testing-library/react-native';
import { I18nextProvider } from 'react-i18next';
import { ThemeProvider } from '../context/ThemeContext';
import i18n from '../i18n';

/**
 * AllProviders mirrors the native .storybook/preview.tsx decorators minus
 * NavigationContainer — stories that need navigation include it in their own
 * decorators (or the meta). Adding it here would nest inside story-level
 * NavigationContainers and crash.
 */
function AllProviders({ children }: { children: React.ReactNode }) {
  return (
    <I18nextProvider i18n={i18n}>
      <ThemeProvider>{children}</ThemeProvider>
    </I18nextProvider>
  );
}

/**
 * Custom render that wraps the component tree in the same providers the app
 * chrome and storybook previews use, so stories and specs render identically.
 *
 * Usage:
 *   const { Default } = composeStories(stories);
 *   const { getByText, getByTestId } = rtlRender(<Default />);
 */
export function rtlRender(
  ui: React.ReactElement,
  options?: RenderOptions,
): ReturnType<typeof render> {
  return render(ui, { wrapper: AllProviders, ...options });
}
