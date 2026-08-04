import React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react-native';
import { BackHandler, Linking } from 'react-native';
import UpdateRequiredScreen from '../UpdateRequiredScreen';

jest.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

jest.mock('react-native-safe-area-context', () => ({
  useSafeAreaInsets: () => ({ top: 0, bottom: 0, left: 0, right: 0 }),
}));

describe('UpdateRequiredScreen', () => {
  const downloadUrl = 'https://example.com/JiAppMobile.apk';

  beforeEach(() => {
    jest.clearAllMocks();
    jest.spyOn(Linking, 'openURL').mockResolvedValue(true);
    jest.spyOn(BackHandler, 'exitApp').mockImplementation(() => {});
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  it('renders title, message and both buttons', () => {
    const { getByTestId, getByText } = render(
      <UpdateRequiredScreen downloadUrl={downloadUrl} />,
    );

    expect(getByTestId('update-required-screen')).toBeTruthy();
    expect(getByText('update.title')).toBeTruthy();
    expect(getByText('update.message')).toBeTruthy();
    expect(getByTestId('update-download-button')).toBeTruthy();
    expect(getByTestId('update-exit-button')).toBeTruthy();
  });

  it('opens the download URL when the download button is pressed', () => {
    const { getByTestId } = render(
      <UpdateRequiredScreen downloadUrl={downloadUrl} />,
    );

    fireEvent.press(getByTestId('update-download-button'));

    expect(Linking.openURL).toHaveBeenCalledWith(downloadUrl);
  });

  it('exits the app when the exit button is pressed', () => {
    const { getByTestId } = render(
      <UpdateRequiredScreen downloadUrl={downloadUrl} />,
    );

    fireEvent.press(getByTestId('update-exit-button'));

    expect(BackHandler.exitApp).toHaveBeenCalled();
  });

  it('shows an inline error when the download link cannot be opened', async () => {
    jest.spyOn(Linking, 'openURL').mockRejectedValue(new Error('No browser'));

    const { getByTestId } = render(
      <UpdateRequiredScreen downloadUrl={downloadUrl} />,
    );

    fireEvent.press(getByTestId('update-download-button'));

    await waitFor(() => {
      expect(getByTestId('update-download-error')).toBeTruthy();
    });
  });
});
