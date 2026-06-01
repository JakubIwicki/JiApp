import React, { useCallback, useEffect, useRef, useState } from 'react';
import {
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from 'react-native';
import Svg, { Circle, Line } from 'react-native-svg';
import { useTranslation } from 'react-i18next';
import { colors, spacing } from '../styles/theme';

interface SearchBarProps {
  onSearch: (query: string) => void;
  initialValue?: string;
  value?: string;
  onChangeText?: (text: string) => void;
  placeholder?: string;
}

const SearchBar: React.FC<SearchBarProps> = ({
  onSearch,
  initialValue = '',
  value: controlledValue,
  onChangeText,
  placeholder,
}) => {
  const { t } = useTranslation();
  const [internalValue, setInternalValue] = useState(initialValue);
  const [isFocused, setIsFocused] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const prevInitialRef = useRef(initialValue);

  const displayValue = controlledValue !== undefined ? controlledValue : internalValue;

  const debouncedSearch = useCallback(
    (text: string) => {
      if (debounceRef.current) {
        clearTimeout(debounceRef.current);
      }
      debounceRef.current = setTimeout(() => {
        onSearch(text);
      }, 500);
    },
    [onSearch],
  );

  const handleChangeText = useCallback(
    (text: string) => {
      if (controlledValue === undefined) {
        setInternalValue(text);
      }
      onChangeText?.(text);
      debouncedSearch(text);
    },
    [debouncedSearch, onChangeText, controlledValue],
  );

  const handleClear = useCallback(() => {
    if (controlledValue === undefined) {
      setInternalValue('');
    }
    onChangeText?.('');
    if (debounceRef.current) {
      clearTimeout(debounceRef.current);
    }
    onSearch('');
  }, [onSearch, onChangeText, controlledValue]);

  const activateSearch = useCallback(() => {
    setIsFocused(true);
  }, []);

  const deactivateSearch = useCallback(() => {
    setIsFocused(false);
  }, []);

  // Sync initialValue for uncontrolled mode when prop changes (render-time pattern)
  if (controlledValue === undefined && initialValue !== prevInitialRef.current) {
    prevInitialRef.current = initialValue;
    setInternalValue(initialValue);
  }

  useEffect(() => {
    const timer = debounceRef.current;
    return () => {
      if (timer) {
        clearTimeout(timer);
      }
    };
  }, [debounceRef]);

  return (
    <View style={styles.container}>
      <View
        style={[styles.inputRow, isFocused && styles.inputRowFocused]}
        testID="search-input-row"
      >
        <Svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke={isFocused ? colors.primary : colors.textTertiary} strokeWidth="2" strokeLinecap="round" style={styles.searchIcon}>
          <Circle cx="11" cy="11" r="8" />
          <Line x1="21" y1="21" x2="16.65" y2="16.65" />
        </Svg>
        <TextInput
          style={styles.input}
          value={displayValue}
          onChangeText={handleChangeText}
          onFocus={activateSearch}
          onBlur={deactivateSearch}
          placeholder={placeholder ?? t('search.placeholder')}
          placeholderTextColor={colors.placeholderDark}
          returnKeyType="search"
          autoCapitalize="none"
          autoCorrect={false}
          testID="search-input"
        />
        {displayValue.length > 0 && (
          <Pressable
            onPress={handleClear}
            style={({ pressed }) => [
              styles.clearButton,
              pressed && { opacity: 0.7 },
            ]}
            testID="search-clear-button"
            accessibilityRole="button"
            accessibilityLabel="Clear search"
          >
            <Text style={styles.clearButtonText}>X</Text>
          </Pressable>
        )}
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    paddingHorizontal: spacing.lg,
    paddingVertical: spacing.md,
    backgroundColor: colors.background,
  },
  inputRow: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: colors.surface,
    borderRadius: 10,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: spacing.md,
    height: 44,
  },
  inputRowFocused: {
    borderColor: colors.primary,
  },
  searchIcon: {
    marginRight: spacing.sm,
  },
  input: {
    flex: 1,
    fontSize: 16,
    color: colors.textPrimary,
    paddingVertical: 0,
  },
  clearButton: {
    marginLeft: spacing.sm,
    width: 24,
    height: 24,
    borderRadius: 12,
    backgroundColor: colors.border,
    justifyContent: 'center',
    alignItems: 'center',
  },
  clearButtonText: {
    color: colors.textTertiary,
    fontSize: 12,
    fontWeight: '700',
  },
});

export default SearchBar;
