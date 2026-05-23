import React, { useState } from 'react';
import { View, Text, ScrollView, StyleSheet, TextInput, Image, TouchableOpacity } from 'react-native';
import Svg, { Circle, Line, Path, Polyline } from 'react-native-svg';
import type { Meta, StoryObj } from '@storybook/react';
import { colors, typography, spacing, borderRadius } from '../styles/theme';
import FloatingParticles from '../components/FloatingParticles';
import Button from '../components/Button';

// ─── Inline feather icons (using react-native-svg) ────────────────────────
const SearchSvg: React.FC<{ color: string; size?: number }> = ({ color, size = 22 }) => (
  <Svg width={size} height={size} viewBox="0 0 24 24" fill="none">
    <Circle cx={10.5} cy={10.5} r={7} stroke={color} strokeWidth={2} strokeLinecap="round" />
    <Line x1={15.5} y1={15.5} x2={21} y2={21} stroke={color} strokeWidth={2} strokeLinecap="round" />
  </Svg>
);

const DownloadsSvg: React.FC<{ color: string; size?: number }> = ({ color, size = 22 }) => (
  <Svg width={size} height={size} viewBox="0 0 24 24" fill="none">
    <Path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4" stroke={color} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
    <Polyline points="7 10 12 15 17 10" stroke={color} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
    <Line x1={12} y1={15} x2={12} y2={3} stroke={color} strokeWidth={2} strokeLinecap="round" />
  </Svg>
);

const HistorySvg: React.FC<{ color: string; size?: number }> = ({ color, size = 22 }) => (
  <Svg width={size} height={size} viewBox="0 0 24 24" fill="none">
    <Circle cx={12} cy={12} r={9} stroke={color} strokeWidth={2} strokeLinecap="round" />
    <Polyline points="12 6 12 12 16 14" stroke={color} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
  </Svg>
);

const SettingsSvg: React.FC<{ color: string; size?: number }> = ({ color, size = 22 }) => (
  <Svg width={size} height={size} viewBox="0 0 24 24" fill="none">
    <Circle cx={12} cy={12} r={4} stroke={color} strokeWidth={2} strokeLinecap="round" />
    <Path d="M12 2v2m0 16v2m10-10h-2M4 12H2m17.07-7.07l-1.41 1.41M6.34 17.66l-1.41 1.41m14.14 0l-1.41-1.41M6.34 6.34L4.93 4.93" stroke={color} strokeWidth={2} strokeLinecap="round" />
  </Svg>
);

const MagnifierSvg: React.FC<{ color: string; size?: number }> = ({ color, size = 18 }) => (
  <Svg width={size} height={size} viewBox="0 0 24 24" fill="none">
    <Circle cx={11} cy={11} r={8} stroke={color} strokeWidth={2} strokeLinecap="round" />
    <Line x1={21} y1={21} x2={16.65} y2={16.65} stroke={color} strokeWidth={2} strokeLinecap="round" />
  </Svg>
);

// ─── Mock data ───────────────────────────────────────────────────────────
const MOCK_RESULTS = [
  {
    videoId: 'dQw4w9WgXcQ',
    title: 'Rick Astley - Never Gonna Give You Up',
    channelTitle: 'Rick Astley',
    imageUrl: 'https://i.ytimg.com/vi/dQw4w9WgXcQ/hqdefault.jpg',
  },
  {
    videoId: 'j5-yKhDd64s',
    title: 'lofi hip hop radio - beats to relax/study to',
    channelTitle: 'Lofi Girl',
    imageUrl: 'https://i.ytimg.com/vi/jfKfPfyJRdk/hqdefault.jpg',
  },
  {
    videoId: '9bZkp7q19f0',
    title: 'PSY - GANGNAM STYLE',
    channelTitle: 'officialpsy',
    imageUrl: 'https://i.ytimg.com/vi/9bZkp7q19f0/hqdefault.jpg',
  },
];

const TAB_ICONS: Record<string, React.FC<{ color: string; size?: number }>> = {
  search: SearchSvg,
  downloads: DownloadsSvg,
  history: HistorySvg,
  settings: SettingsSvg,
};

// ─── The full app shell ──────────────────────────────────────────────────
const FullAppShell: React.FC<{ activeTab?: string }> = ({ activeTab = 'search' }) => {
  const [query, setQuery] = useState('lo-fi beats');
  const [activeTabState, setActiveTabState] = useState(activeTab);

  const tabs = [
    { key: 'search', label: 'Search' },
    { key: 'downloads', label: 'Downloads' },
    { key: 'history', label: 'History' },
    { key: 'settings', label: 'Settings' },
  ];

  const renderContent = () => {
    switch (activeTabState) {
      case 'search':
        return <SearchContent query={query} setQuery={setQuery} />;
      case 'downloads':
        return <DownloadsContent />;
      case 'history':
        return <HistoryContent />;
      case 'settings':
        return <SettingsContent />;
      default:
        return null;
    }
  };

  return (
    <View style={fullStyles.shell}>
      <View style={fullStyles.content}>{renderContent()}</View>
      <View style={fullStyles.tabBar}>
        {tabs.map((tab) => {
          const isActive = activeTabState === tab.key;
          const IconComponent = TAB_ICONS[tab.key];
          return (
            <TouchableOpacity
              key={tab.key}
              style={fullStyles.tab}
              onPress={() => setActiveTabState(tab.key)}
              activeOpacity={0.6}
            >
              <IconComponent color={isActive ? '#8B7E74' : '#C0B8AE'} size={22} />
              <Text style={[fullStyles.tabLabel, { color: isActive ? '#8B7E74' : '#C0B8AE', fontWeight: isActive ? ('600' as const) : ('400' as const) }]}>
                {tab.label}
              </Text>
            </TouchableOpacity>
          );
        })}
      </View>
    </View>
  );
};

// ─── Search Tab Content ──────────────────────────────────────────────────
const SearchContent: React.FC<{ query: string; setQuery: (q: string) => void }> = ({ query, setQuery }) => (
  <View style={fullStyles.screen}>
    <FloatingParticles count={5} />
    <Text style={fullStyles.screenTitle}>JiApp</Text>
    <View style={fullStyles.searchBox}>
      <MagnifierSvg color="#8B7E74" size={18} />
      <TextInput
        style={fullStyles.searchInput}
        value={query}
        onChangeText={setQuery}
        placeholder="Search YouTube videos..."
        placeholderTextColor="#B5ACA0"
      />
      {query.length > 0 && (
        <TouchableOpacity onPress={() => setQuery('')} style={fullStyles.clearBtn}>
          <Text style={fullStyles.clearBtnText}>✕</Text>
        </TouchableOpacity>
      )}
    </View>
    <Text style={fullStyles.sectionLabel}>Results</Text>
    <ScrollView style={fullStyles.resultsList}>
      {MOCK_RESULTS.map((video) => (
        <View key={video.videoId} style={fullStyles.card}>
          <Image source={{ uri: video.imageUrl }} style={fullStyles.thumb} />
          <View style={fullStyles.cardInfo}>
            <Text style={fullStyles.cardTitle} numberOfLines={2}>{video.title}</Text>
            <Text style={fullStyles.cardMeta}>{video.channelTitle}</Text>
          </View>
        </View>
      ))}
    </ScrollView>
  </View>
);

// ─── Downloads Tab Content ───────────────────────────────────────────────
const DownloadsContent: React.FC = () => (
  <View style={fullStyles.screen}>
    <Text style={fullStyles.screenTitle}>Downloads</Text>
    <Text style={fullStyles.sectionLabel}>Downloading</Text>
    <View style={fullStyles.card}>
      <View style={[fullStyles.thumb, { backgroundColor: colors.primaryLight }]} />
      <View style={fullStyles.cardInfo}>
        <Text style={fullStyles.cardTitle} numberOfLines={2}>lofi hip hop radio - beats to study</Text>
        <View style={fullStyles.progressBar}>
          <View style={[fullStyles.progressFill, { width: '45%' }]} />
        </View>
        <Text style={fullStyles.cardMeta}>2.4 MB / 5.8 MB</Text>
      </View>
    </View>
    <Text style={[fullStyles.sectionLabel, { marginTop: 12 }]}>Completed</Text>
    <View style={fullStyles.card}>
      <Image source={{ uri: 'https://i.ytimg.com/vi/9bZkp7q19f0/hqdefault.jpg' }} style={fullStyles.thumb} />
      <View style={fullStyles.cardInfo}>
        <Text style={fullStyles.cardTitle} numberOfLines={2}>PSY - GANGNAM STYLE</Text>
        <Text style={[fullStyles.cardMeta, { color: colors.success }]}>✓ Downloaded · 4.2 MB</Text>
      </View>
    </View>
  </View>
);

// ─── History Tab Content ─────────────────────────────────────────────────
const HistoryContent: React.FC = () => (
  <View style={fullStyles.screen}>
    <Text style={fullStyles.screenTitle}>History</Text>
    <Text style={fullStyles.sectionLabel}>Recent Searches</Text>
    <View style={fullStyles.card}>
      <View style={fullStyles.searchIconCircle}>
        <MagnifierSvg color="#A0998F" size={16} />
      </View>
      <View style={fullStyles.cardInfo}>
        <Text style={fullStyles.cardTitle}>lo-fi beats</Text>
        <Text style={fullStyles.cardMeta}>21.05.2026</Text>
      </View>
    </View>
    <View style={fullStyles.card}>
      <View style={fullStyles.searchIconCircle}>
        <MagnifierSvg color="#A0998F" size={16} />
      </View>
      <View style={fullStyles.cardInfo}>
        <Text style={fullStyles.cardTitle}>japanese lofi sakura</Text>
        <Text style={fullStyles.cardMeta}>20.05.2026</Text>
      </View>
    </View>
    <Text style={[fullStyles.sectionLabel, { marginTop: 12 }]}>Recent Downloads</Text>
    <View style={fullStyles.card}>
      <Image source={{ uri: 'https://i.ytimg.com/vi/jfKfPfyJRdk/hqdefault.jpg' }} style={fullStyles.thumb} />
      <View style={fullStyles.cardInfo}>
        <Text style={fullStyles.cardTitle} numberOfLines={2}>lofi hip hop radio - beats to study</Text>
        <Text style={fullStyles.cardMeta}>20.05.2026 · 5.8 MB</Text>
      </View>
    </View>
  </View>
);

// ─── Settings Tab Content ────────────────────────────────────────────────
const SettingsContent: React.FC = () => {
  const [lang, setLang] = React.useState<'pl' | 'en'>('pl');

  return (
    <View style={fullStyles.screen}>
      <Text style={fullStyles.screenTitle}>Settings</Text>
      <Text style={fullStyles.sectionLabel}>Language</Text>
      <View style={fullStyles.settingsRow}>
        <Text style={fullStyles.settingsLabel}>Język / Language</Text>
        <View style={fullStyles.langPill}>
          <TouchableOpacity
            onPress={() => setLang('pl')}
            activeOpacity={lang === 'pl' ? 1 : 0.6}
            style={[fullStyles.langOption, lang === 'pl' && fullStyles.langActive]}
          >
            <Text style={lang === 'pl' ? fullStyles.langActiveText : fullStyles.langInactiveText}>PL</Text>
          </TouchableOpacity>
          <TouchableOpacity
            onPress={() => setLang('en')}
            activeOpacity={lang === 'en' ? 1 : 0.6}
            style={[fullStyles.langOption, lang === 'en' && fullStyles.langActive]}
          >
            <Text style={lang === 'en' ? fullStyles.langActiveText : fullStyles.langInactiveText}>EN</Text>
          </TouchableOpacity>
        </View>
      </View>
      <Text style={[fullStyles.sectionLabel, { marginTop: 8 }]}>Account</Text>
      <View style={fullStyles.accountCard}>
        <View style={fullStyles.accountRow}>
          <Text style={fullStyles.accountLabel}>Name</Text>
          <Text style={fullStyles.accountValue}>Jakub Iwicki</Text>
        </View>
        <View style={[fullStyles.accountRow, fullStyles.accountRowBorder]}>
          <Text style={fullStyles.accountLabel}>Username</Text>
          <Text style={fullStyles.accountValue}>jiwicki</Text>
        </View>
      </View>
      <TouchableOpacity style={fullStyles.logoutBtn}>
        <Text style={fullStyles.logoutText}>Log Out</Text>
      </TouchableOpacity>
      <Text style={fullStyles.version}>Version 1.0.0</Text>
    </View>
  );
};

// ─── Story Meta ──────────────────────────────────────────────────────────
const meta: Meta<typeof FullAppShell> = {
  title: 'Screens/FullApp',
  component: FullAppShell,
};

export default meta;
type Story = StoryObj<typeof FullAppShell>;

export const SearchWithResults: Story = {
  args: { activeTab: 'search' },
};

export const DownloadsTab: Story = {
  args: { activeTab: 'downloads' },
};

export const HistoryTab: Story = {
  args: { activeTab: 'history' },
};

export const SettingsTab: Story = {
  args: { activeTab: 'settings' },
};

// ─── Styles ──────────────────────────────────────────────────────────────
const fullStyles = StyleSheet.create({
  shell: { flex: 1, backgroundColor: colors.background, minHeight: 600, maxWidth: 390, alignSelf: 'center', width: '100%' },
  content: { flex: 1 },
  screen: { flex: 1, paddingHorizontal: spacing.lg },
  screenTitle: { ...typography.title, paddingTop: spacing.lg, paddingBottom: spacing.md },

  searchBox: {
    flexDirection: 'row', alignItems: 'center', backgroundColor: colors.surface,
    borderWidth: 1, borderColor: colors.primary, borderRadius: 14, paddingHorizontal: spacing.md, height: 44, marginBottom: spacing.md,
  },
  searchInput: { flex: 1, fontSize: 15, color: colors.textPrimary, marginLeft: spacing.sm },
  clearBtn: { width: 20, height: 20, borderRadius: 10, backgroundColor: colors.border, alignItems: 'center', justifyContent: 'center' },
  clearBtnText: { fontSize: 10, color: colors.textTertiary, fontWeight: '700' },

  sectionLabel: { fontSize: 11, fontWeight: '600', color: colors.textTertiary, textTransform: 'uppercase', letterSpacing: 1, marginBottom: spacing.sm },
  resultsList: { flex: 1 },

  card: {
    flexDirection: 'row', backgroundColor: colors.surface, borderRadius: borderRadius.lg,
    padding: 10, marginBottom: 8, borderWidth: 1, borderColor: '#F0EAE4',
    shadowColor: colors.cardShadow, shadowOffset: { width: 0, height: 1 }, shadowOpacity: 0.06, shadowRadius: 3, elevation: 2,
  },
  thumb: { width: 96, height: 64, borderRadius: 8, backgroundColor: colors.primaryLight },
  cardInfo: { flex: 1, marginLeft: 10, justifyContent: 'center' },
  cardTitle: { fontSize: 13, fontWeight: '600', color: colors.textPrimary, lineHeight: 18, marginBottom: 3 },
  cardMeta: { fontSize: 11, color: colors.textTertiary },

  progressBar: { height: 3, backgroundColor: colors.primaryLight, borderRadius: 2, marginVertical: 4 },
  progressFill: { height: '100%', backgroundColor: colors.primary, borderRadius: 2 },

  searchIconCircle: { width: 36, height: 36, borderRadius: 18, backgroundColor: colors.primaryLight, alignItems: 'center', justifyContent: 'center' },

  settingsRow: {
    flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center',
    backgroundColor: colors.surface, borderRadius: borderRadius.lg, paddingHorizontal: spacing.lg, paddingVertical: 14,
  },
  settingsLabel: { fontSize: 15, color: colors.textPrimary },
  langPill: { flexDirection: 'row', backgroundColor: colors.primaryLight, borderRadius: 10, padding: 3 },
  langOption: { paddingHorizontal: 14, paddingVertical: 6, borderRadius: 8 },
  langActive: { backgroundColor: colors.primary },
  langActiveText: { fontSize: 14, fontWeight: '600', color: colors.textInverse },
  langInactiveText: { fontSize: 14, fontWeight: '600', color: colors.textTertiary },

  accountCard: { backgroundColor: colors.surface, borderRadius: borderRadius.lg, paddingHorizontal: spacing.lg, marginTop: spacing.sm },
  accountRow: { flexDirection: 'row', justifyContent: 'space-between', paddingVertical: 12 },
  accountRowBorder: { borderTopWidth: 1, borderTopColor: '#F0EAE4' },
  accountLabel: { fontSize: 13, color: colors.textTertiary },
  accountValue: { fontSize: 13, color: colors.textPrimary, fontWeight: '500' },

  logoutBtn: {
    marginTop: spacing.lg, marginHorizontal: spacing.md, borderWidth: 1.5, borderColor: colors.error,
    borderRadius: borderRadius.lg, paddingVertical: 12, alignItems: 'center',
  },
  logoutText: { fontSize: 15, color: colors.error, fontWeight: '500' },
  version: { textAlign: 'center', fontSize: 12, color: colors.textTertiary, marginTop: spacing.lg, marginBottom: spacing.xxl },

  tabBar: {
    flexDirection: 'row', justifyContent: 'space-around', paddingTop: 10, paddingBottom: 16,
    borderTopWidth: 1, borderTopColor: colors.separator, backgroundColor: colors.surface,
  },
  tab: { alignItems: 'center', gap: 3 },
  tabLabel: { fontSize: 9 },
});
