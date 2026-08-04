import React from 'react';
import { StyleSheet, View } from 'react-native';
import type { Meta, StoryObj } from '@storybook/react';
import UpdateRequiredScreen from './UpdateRequiredScreen';

const meta: Meta<typeof UpdateRequiredScreen> = {
  title: 'Components/UpdateRequiredScreen',
  component: UpdateRequiredScreen,
  decorators: [
    Story => (
      <View style={styles.decorator}>
        <Story />
      </View>
    ),
  ],
};

export default meta;

type Story = StoryObj<typeof UpdateRequiredScreen>;

export const Default: Story = {
  args: {
    downloadUrl:
      'https://jiapp-downloads-899088266605.s3.eu-central-1.amazonaws.com/JiAppMobile.apk',
  },
};

const styles = StyleSheet.create({
  decorator: {
    flex: 1,
  },
});
