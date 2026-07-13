/**
 * Centralized mock-service container.
 *
 * Aggregates all service mocks so specs and stories can program the SAME layer
 * with a single import. Each mock service lives in the standard __mocks__/
 * directory (auto-discovered by jest) and exposes jest.fn() wrappers plus
 * .withX() scenario builders and a .reset().
 *
 * Usage in a spec (jest):
 *   import { mockServices } from '../../test/mocks/mockServices';
 *   jest.mock('../../services/authService');        // jest → __mocks__
 *   jest.mock('../../services/searchService');       // jest → __mocks__
 *   beforeEach(() => mockServices.resetAll());
 *   mockServices.auth.withLoginSuccess();
 *
 * Usage in a story (storybook-web / Vite):
 *   import { mockServices } from '../test/mocks/mockServices';
 *   mockServices.auth.withLoginSuccess();
 *   // The Vite plugin redirects service imports to __mocks__/*,
 *   // so the component reads the same programmed state.
 */
import * as auth from '../../services/__mocks__/authService';
import * as search from '../../services/__mocks__/searchService';
import * as download from '../../services/__mocks__/downloadService';
import * as history from '../../services/__mocks__/historyService';

export const mockServices = {
  auth,
  search,
  download,
  history,

  resetAll() {
    auth.reset();
    search.reset();
    download.reset();
    history.reset();
  },
};

export type MockServices = typeof mockServices;
