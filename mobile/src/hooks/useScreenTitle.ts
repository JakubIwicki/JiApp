import { useLayoutEffect } from 'react';
import { useNavigation } from '@react-navigation/native';
import { useTranslation } from 'react-i18next';

const useScreenTitle = (titleKey: string) => {
  const { t } = useTranslation();
  const navigation = useNavigation();

  useLayoutEffect(() => {
    navigation.setOptions({ title: t(titleKey) });
  }, [navigation, t, titleKey]);
};

export default useScreenTitle;
