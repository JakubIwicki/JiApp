import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { saveLanguage } from '../services/storageService';

interface UseLanguageResult {
  currentLanguage: string;
  changeLanguage: (language: string) => Promise<void>;
}

const useLanguage = (): UseLanguageResult => {
  const { i18n } = useTranslation();

  const currentLanguage = i18n.language?.startsWith('pl') ? 'pl' : 'en';

  const changeLanguage = useCallback(
    async (language: string) => {
      await i18n.changeLanguage(language);
      await saveLanguage(language);
    },
    [i18n],
  );

  return { currentLanguage, changeLanguage };
};

export default useLanguage;
