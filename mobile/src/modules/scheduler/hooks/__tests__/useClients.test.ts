import { renderHook, act } from '@testing-library/react-native';
import useClients from '../useClients';
import * as clientService from '../../services/clientService';

jest.mock('../../services/clientService', () => ({
  listClients: jest.fn(),
  createClient: jest.fn(),
  deleteClient: jest.fn(),
}));

const mockListClients = clientService.listClients as jest.Mock;
const mockCreateClient = clientService.createClient as jest.Mock;
const mockDeleteClient = clientService.deleteClient as jest.Mock;
const boardId = 1;

describe('useClients', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('has initial state', () => {
    const { result } = renderHook(() => useClients(boardId));
    expect(result.current.clients).toEqual([]);
    expect(result.current.isLoading).toBe(false);
    expect(result.current.error).toBeNull();
  });

  it('loads clients on search', async () => {
    mockListClients.mockResolvedValue([
      { id: 1, name: 'Jane' },
      { id: 2, name: 'John' },
    ]);

    const { result } = renderHook(() => useClients(boardId));

    await act(async () => {
      await result.current.searchClients('Jane');
    });

    expect(mockListClients).toHaveBeenCalledWith(boardId, 'Jane');
    expect(result.current.clients).toHaveLength(2);
    expect(result.current.isLoading).toBe(false);
  });

  it('loads all clients when no search query', async () => {
    mockListClients.mockResolvedValue([]);

    const { result } = renderHook(() => useClients(boardId));

    await act(async () => {
      await result.current.loadAll();
    });

    expect(mockListClients).toHaveBeenCalledWith(boardId, undefined);
    expect(result.current.isLoading).toBe(false);
  });

  it('handles search error', async () => {
    mockListClients.mockRejectedValue(new Error('Search failed'));

    const { result } = renderHook(() => useClients(boardId));

    await act(async () => {
      await result.current.searchClients('test');
    });

    expect(result.current.error).toBe('Search failed');
    expect(result.current.clients).toEqual([]);
  });

  it('adds a client and refreshes', async () => {
    mockCreateClient.mockResolvedValue({ id: 5 });
    mockListClients.mockResolvedValue([{ id: 5, name: 'New Client' }]);

    const { result } = renderHook(() => useClients(boardId));

    await act(async () => {
      await result.current.addClient({ name: 'New Client' });
    });

    expect(mockCreateClient).toHaveBeenCalledWith(boardId, { name: 'New Client' });
  });

  it('removes a client', async () => {
    mockDeleteClient.mockResolvedValue(undefined);

    const { result } = renderHook(() => useClients(boardId));

    await act(async () => {
      await result.current.removeClient(1);
    });

    expect(mockDeleteClient).toHaveBeenCalledWith(1);
  });
});
