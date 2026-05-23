import i18next from 'i18next';
import { initReactI18next } from 'react-i18next';
import { getLocales } from 'react-native-localize';
import { getLanguage } from '../services/storageService';
import pl from './pl.json';
import en from './en.json';

const deviceLanguage = getLocales()[0]?.languageCode ?? 'pl';
const supportedLanguages = ['pl', 'en'];
const fallbackLanguage = 'pl';

i18next.use(initReactI18next).init({
  resources: {
    pl: { translation: pl },
    en: { translation: en },
  },
  lng: supportedLanguages.includes(deviceLanguage) ? deviceLanguage : fallbackLanguage,
  fallbackLng: fallbackLanguage,
  interpolation: {
    escapeValue: false,
  },
});

// After init, check for a persisted language preference and apply it.
// This runs asynchronously; UI will switch once changeLanguage resolves.
getLanguage().then((savedLanguage) => {
  if (savedLanguage && supportedLanguages.includes(savedLanguage)) {
    i18next.changeLanguage(savedLanguage);
  }
});

export default i18next;
