import { start } from '@storybook/react-native';

const view = start({
  storyEntries: [],
  annotations: [],
});

const Storybook = view.getStorybookUI({});

export default Storybook;
