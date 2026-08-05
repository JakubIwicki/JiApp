import React, { useMemo } from 'react';
import useLanguage from '../hooks/useLanguage';
import SegmentedControl from './SegmentedControl';

const LanguagePicker: React.FC = () => {
  const { currentLanguage, changeLanguage } = useLanguage();

  const options = useMemo(
    () => [
      { value: 'pl', label: 'PL' },
      { value: 'en', label: 'EN' },
    ],
    [],
  );

  return (
    <SegmentedControl
      options={options}
      value={currentLanguage}
      onChange={changeLanguage}
      testID="language-picker-button"
    />
  );
};

export default LanguagePicker;
