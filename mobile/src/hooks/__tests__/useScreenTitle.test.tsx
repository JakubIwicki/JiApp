import { renderHook } from '@testing-library/react-native';

// Mock react-i18next
jest.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

// Mock @react-navigation/native
const mockSetOptions = jest.fn();
jest.mock('@react-navigation/native', () => ({
  useNavigation: () => ({
    setOptions: mockSetOptions,
  }),
}));

import useScreenTitle from '../useScreenTitle';

describe('useScreenTitle', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('sets navigation title using translated key', () => {
    renderHook(() => useScreenTitle('auth.loginTitle'));
    expect(mockSetOptions).toHaveBeenCalledWith({ title: 'auth.loginTitle' });
  });

  it('updates title when titleKey changes', () => {
    const { rerender } = renderHook(
      ({ titleKey }: { titleKey: string }) => useScreenTitle(titleKey),
      { initialProps: { titleKey: 'auth.loginTitle' } },
    );

    rerender({ titleKey: 'auth.registerTitle' });

    expect(mockSetOptions).toHaveBeenCalledTimes(2);
    expect(mockSetOptions).toHaveBeenLastCalledWith({
      title: 'auth.registerTitle',
    });
  });
});
