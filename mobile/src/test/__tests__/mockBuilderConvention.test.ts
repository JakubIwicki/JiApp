import * as appointmentServiceMock from '../../modules/scheduler/services/__mocks__/appointmentService';
import * as clientServiceMock from '../../modules/scheduler/services/__mocks__/clientService';
import * as expenseServiceMock from '../../modules/scheduler/services/__mocks__/expenseService';
import * as reportServiceMock from '../../modules/scheduler/services/__mocks__/reportService';
import * as serviceCatalogServiceMock from '../../modules/scheduler/services/__mocks__/serviceCatalogService';
import * as boardServiceMock from '../../modules/lovingBoards/services/__mocks__/boardService';
import * as itemServiceMock from '../../modules/lovingBoards/services/__mocks__/itemService';
import * as adminServiceMock from '../../modules/admin/services/__mocks__/adminService';
import * as previewServiceMock from '../../services/__mocks__/previewService';
import * as authServiceMock from '../../services/__mocks__/authService';
import * as searchServiceMock from '../../services/__mocks__/searchService';
import * as downloadServiceMock from '../../services/__mocks__/downloadService';
import * as historyServiceMock from '../../services/__mocks__/historyService';

interface MockModule {
  name: string;
  module: Record<string, unknown>;
}

// G12 fitness guard: ALL 13 service test doubles under mobile/src/**/__mocks__
// (5 scheduler + 2 lovingBoards + 1 admin + 1 preview + 4 core services) must
// be builder-style semantic doubles — no mode-flag setters (setXMode), a
// reset() function, and at least one withX() builder that is actually
// callable. The list below is a hardcoded enumeration: adding a new service
// test double requires adding it to MOCK_MODULES by hand — the guard does NOT
// auto-discover __mocks__ dirs. The length check keeps the scan from passing
// vacuously on an empty list, and a mock whose import fails fails the whole
// test file.
const MOCK_MODULES: MockModule[] = [
  {
    name: 'appointmentService',
    module: appointmentServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'clientService',
    module: clientServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'expenseService',
    module: expenseServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'reportService',
    module: reportServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'serviceCatalogService',
    module: serviceCatalogServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'boardService',
    module: boardServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'itemService',
    module: itemServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'adminService',
    module: adminServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'previewService',
    module: previewServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'authService',
    module: authServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'searchService',
    module: searchServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'downloadService',
    module: downloadServiceMock as unknown as Record<string, unknown>,
  },
  {
    name: 'historyService',
    module: historyServiceMock as unknown as Record<string, unknown>,
  },
];

describe('mock builder convention', () => {
  it('scans at least one mock module', () => {
    expect(MOCK_MODULES.length).toBeGreaterThan(0);
  });

  it.each(MOCK_MODULES)(
    '$name exports a builder-style double (no setXMode, callable reset, callable withX)',
    ({ module }) => {
      const exportNames = Object.keys(module);

      expect(
        exportNames.some(exportName => /^set\w+Mode$/.test(exportName)),
      ).toBe(false);

      const resetExport = module.reset;
      expect(typeof resetExport).toBe('function');

      const builderNames = exportNames.filter(exportName =>
        /^with[A-Z]\w*$/.test(exportName),
      );
      expect(builderNames.length).toBeGreaterThan(0);
      expect(
        builderNames.some(
          builderName => typeof module[builderName] === 'function',
        ),
      ).toBe(true);
    },
  );
});
