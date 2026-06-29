import React, { useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import * as storageService from '../services/storageService';
import SegmentedControl from './SegmentedControl';

const LanguagePicker: React.FC = () => {
  const { i18n } = useTranslation();

  const currentLanguage = i18n.language?.startsWith('pl') ? 'pl' : 'en';

  const options = useMemo(
    () => [
      { value: 'pl', label: 'PL' },
      { value: 'en', label: 'EN' },
    ],
    [],
  );

  const handleChange = useCallback(
    async (value: string) => {
      await i18n.changeLanguage(value);
      await storageService.saveLanguage(value);
    },
    [i18n],
  );

  return (
    <SegmentedControl
      options={options}
      value={currentLanguage}
      onChange={handleChange}
      testID="language-picker-button"
    />
  );
};

export default LanguagePicker;
