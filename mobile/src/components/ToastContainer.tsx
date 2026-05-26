import React, { useContext } from 'react';
import { StyleSheet, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useTranslation } from 'react-i18next';
import Toast from './Toast';
import { ToastContext } from '../context/ToastContext';

const ToastContainer: React.FC = () => {
  const { queue, popToast } = useContext(ToastContext);
  const insets = useSafeAreaInsets();
  const { t } = useTranslation();

  return (
    <View
      style={[
        styles.container,
        { top: insets.top + 8, left: 16, right: 16 },
      ]}
      pointerEvents="box-none"
    >
      {queue.map((toast) => (
        <View key={toast.id} style={styles.item}>
          <Toast
            type={toast.type}
            title={t(toast.titleKey)}
            description={toast.descKey ? t(toast.descKey) : undefined}
            persistent={toast.persistent}
            onDismiss={() => popToast(toast.id)}
          />
        </View>
      ))}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    position: 'absolute',
    zIndex: 9999,
  },
  item: {
    marginBottom: 8,
  },
});

export default ToastContainer;
