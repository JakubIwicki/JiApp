import { useCallback, useContext } from 'react';
import { ToastContext } from '../context/ToastContext';

interface UseToastResult {
  showSuccess: (titleKey: string, descKey?: string) => void;
  showError: (titleKey: string, descKey?: string) => void;
  showInfo: (titleKey: string, descKey?: string) => void;
  showWarning: (titleKey: string, descKey?: string) => void;
}

const useToast = (): UseToastResult => {
  const { pushToast } = useContext(ToastContext);

  const showSuccess = useCallback(
    (titleKey: string, descKey?: string) => {
      pushToast({ type: 'success', titleKey, descKey, persistent: false });
    },
    [pushToast],
  );

  const showError = useCallback(
    (titleKey: string, descKey?: string) => {
      pushToast({ type: 'error', titleKey, descKey, persistent: true });
    },
    [pushToast],
  );

  const showInfo = useCallback(
    (titleKey: string, descKey?: string) => {
      pushToast({ type: 'info', titleKey, descKey, persistent: false });
    },
    [pushToast],
  );

  const showWarning = useCallback(
    (titleKey: string, descKey?: string) => {
      pushToast({ type: 'warning', titleKey, descKey, persistent: true });
    },
    [pushToast],
  );

  return { showSuccess, showError, showInfo, showWarning };
};

export default useToast;
