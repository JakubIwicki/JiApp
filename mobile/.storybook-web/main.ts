import { existsSync } from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { StorybookConfig } from '@storybook/react-vite';

const dirname = path.dirname(fileURLToPath(import.meta.url));
const projectDir = path.resolve(dirname, '..');
const mocksDir = path.resolve(dirname, '../src/__mocks__');
const servicesDir = path.resolve(dirname, '../src/services');
const modulesDir = path.resolve(dirname, '../src/modules');

const config: StorybookConfig = {
  stories: ['../src/**/*.stories.tsx'],

  addons: [],

  framework: {
    name: '@storybook/react-vite',
    options: {},
  },

  async viteFinal(viteConfig) {
    viteConfig.resolve = viteConfig.resolve ?? {};
    viteConfig.resolve.alias = {
      ...viteConfig.resolve.alias,
      // Core: RN → RNW
      'react-native': 'react-native-web',

      // Native-only module stubs
      'react-native-encrypted-storage': path.join(
        mocksDir,
        'react-native-encrypted-storage.ts',
      ),
      'react-native-blob-util': path.join(
        mocksDir,
        'react-native-blob-util.ts',
      ),
      'react-native-localize': path.join(mocksDir, 'react-native-localize.ts'),
      '@react-native-async-storage/async-storage': path.join(
        mocksDir,
        '@react-native-async-storage/async-storage.ts',
      ),

      // Mock the entire navigation layer — avoids pulling in react-native-screens
      // which has native-only code that can't run on web.
      '@react-navigation/native': path.join(mocksDir, 'react-navigation.tsx'),
      '@react-navigation/stack': path.join(mocksDir, 'react-navigation.tsx'),
      '@react-navigation/bottom-tabs': path.join(
        mocksDir,
        'react-navigation.tsx',
      ),
      // react-native-svg's Fabric native components import codegenNativeComponent
      // which doesn't exist in react-native-web. The mock below renders real DOM
      // <svg> elements instead — icons are visible, Fabric imports are avoided.
      'react-native-svg': path.join(mocksDir, 'react-native-svg.tsx'),

      // react-native-markdown-display contains Flow-annotated source that Vite
      // can't parse. The stub renders markdown content as plain <Text>.
      'react-native-markdown-display': path.join(
        mocksDir,
        'react-native-markdown-display.tsx',
      ),
    };

    // Plugin: redirect real service imports to their sibling __mocks__/ file
    // (never apiClient or storageService). Covers both src/services/* and
    // src/modules/*/services/*. Only redirects when the sibling mock exists so
    // unmocked services (e.g. scheduler boardService, lovingBoards
    // boardStreamService) resolve normally instead of pointing at a missing
    // module.
    viteConfig.plugins = viteConfig.plugins ?? [];
    viteConfig.plugins.push({
      name: 'mock-services',
      enforce: 'pre',
      resolveId(source, importer) {
        if (!importer) return null;
        // Only intercept relative imports — bare specifiers (node_modules
        // packages) should resolve normally.
        if (!source.startsWith('.')) return null;
        const resolved = path.resolve(path.dirname(importer), source);
        // Guard: reject paths that escape the project directory
        if (!resolved.startsWith(projectDir + path.sep)) return null;
        const isCoreService = resolved.startsWith(servicesDir);
        const isModuleService =
          resolved.startsWith(modulesDir) &&
          resolved.includes(`${path.sep}services${path.sep}`);
        if (
          (isCoreService || isModuleService) &&
          !resolved.includes('__mocks__') &&
          !resolved.includes('apiClient') &&
          !resolved.includes('storageService')
        ) {
          const ext = path.extname(resolved) || '.ts';
          const mockPath = path.resolve(
            path.dirname(resolved),
            '__mocks__',
            path.basename(resolved, ext) + ext,
          );
          return existsSync(mockPath) ? mockPath : null;
        }
        return null;
      },
    });

    return viteConfig;
  },
};

export default config;
