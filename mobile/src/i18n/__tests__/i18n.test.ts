import pl from '../pl.json';
import en from '../en.json';
import i18next from '../index';

type TranslationObject = {
  [key: string]: string | TranslationObject;
};

/**
 * Recursively collect all leaf keys from a nested translation object.
 * Returns an array of dot-notation paths, e.g. ["auth.login", "auth.username"].
 */
function collectLeafKeys(
  obj: TranslationObject,
  prefix = '',
): string[] {
  const keys: string[] = [];

  for (const key of Object.keys(obj).sort()) {
    const value = obj[key];
    const fullPath = prefix ? `${prefix}.${key}` : key;

    if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
      keys.push(...collectLeafKeys(value as TranslationObject, fullPath));
    } else {
      keys.push(fullPath);
    }
  }

  return keys;
}

describe('i18n translations', () => {
  describe('pl.json and en.json key parity', () => {
    it('pl.json and en.json have identical sets of keys', () => {
      const plKeys = collectLeafKeys(pl as unknown as TranslationObject);
      const enKeys = collectLeafKeys(en as unknown as TranslationObject);

      // Check each key individually for better error messages
      for (const key of plKeys) {
        expect(enKeys).toContain(key);
      }

      for (const key of enKeys) {
        expect(plKeys).toContain(key);
      }

      expect(plKeys).toEqual(enKeys);
    });
  });

  describe('i18next initialization', () => {
    it('initializes with Polish as fallback language', () => {
      expect(i18next.options.fallbackLng).toEqual(['pl']);
    });

    it('has pl and en resources registered', () => {
      const languages = i18next.languages;
      expect(languages).toContain('pl');
      expect(languages).toContain('en');
    });
  });

  describe('language switching', () => {
    it('changeLanguage("en") switches all t() calls to English', () => {
      // Switch to English
      i18next.changeLanguage('en');
      expect(i18next.t('auth.login')).toBe('Log In');
      expect(i18next.t('auth.register')).toBe('Sign Up');
      expect(i18next.t('nav.search')).toBe('Search');
      expect(i18next.t('common.loading')).toBe('Loading...');
    });

    it('changeLanguage("pl") switches all t() calls to Polish', () => {
      i18next.changeLanguage('pl');
      expect(i18next.t('auth.login')).toBe('Zaloguj się');
      expect(i18next.t('auth.register')).toBe('Zarejestruj się');
      expect(i18next.t('nav.search')).toBe('Szukaj');
      expect(i18next.t('common.loading')).toBe('Ładowanie...');
    });
  });
});
