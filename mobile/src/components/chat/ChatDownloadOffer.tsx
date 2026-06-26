import React from 'react';
import { StyleSheet, View } from 'react-native';
import { useTranslation } from 'react-i18next';
import VideoCard from '../VideoCard';
import Button from '../Button';
import { useThemedStyles } from '../../context/ThemeContext';
import type { Theme } from '../../styles/theme';
import { spacing } from '../../styles/theme';
import type { VideoItem } from '../../types/api';
import type { DownloadOfferData } from '../../types/chat';

interface ChatDownloadOfferProps {
  readonly offer: DownloadOfferData;
  readonly status: 'idle' | 'downloading' | 'done' | 'error';
  readonly onConfirm: () => void;
}

const ChatDownloadOffer: React.FC<ChatDownloadOfferProps> = ({
  offer,
  status,
  onConfirm,
}) => {
  const { t } = useTranslation();
  const styles = useThemedStyles(makeStyles);

  const syntheticVideo: VideoItem = {
    videoId: offer.videoId,
    title: offer.title ?? offer.videoId,
    description: '',
    imageUrl: offer.imageUrl ?? '',
    videoUrl: offer.videoUrl,
    channelTitle: '',
  };

  const buttonTitle =
    status === 'idle'
      ? t('chat.offer.download')
      : status === 'downloading'
      ? t('chat.offer.downloading')
      : status === 'done'
      ? t('chat.offer.done')
      : t('chat.offer.retry');

  const isButtonDisabled = status === 'downloading' || status === 'done';
  const isButtonLoading = status === 'downloading';

  return (
    <View style={styles.container}>
      <View style={styles.divider} />
      <VideoCard video={syntheticVideo} onPress={() => {}} />
      <View style={styles.buttonRow}>
        <Button
          title={buttonTitle}
          onPress={onConfirm}
          disabled={isButtonDisabled}
          loading={isButtonLoading}
          variant="primary"
        />
      </View>
    </View>
  );
};

const makeStyles = (t: Theme) =>
  StyleSheet.create({
    container: {
      marginTop: spacing.sm,
    },
    divider: {
      height: StyleSheet.hairlineWidth,
      backgroundColor: t.colors.separator,
      marginHorizontal: spacing.lg,
      marginBottom: spacing.sm,
    },
    buttonRow: {
      paddingHorizontal: spacing.lg,
      paddingTop: spacing.sm,
    },
  });

export default ChatDownloadOffer;
