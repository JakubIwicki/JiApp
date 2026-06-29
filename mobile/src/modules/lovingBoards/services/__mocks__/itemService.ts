type Mode = 'success' | 'empty' | 'error';

let _mode: Mode = 'success';

export const setItemMode = (mode: Mode) => {
  _mode = mode;
};

export const createItem = async (
  _boardId: number,
  _payload: unknown,
): Promise<{ id: number }> => {
  if (_mode === 'error') throw new Error('Mock error');
  return { id: 99 };
};

export const updateItem = async (
  _boardId: number,
  _itemId: number,
  _payload: unknown,
): Promise<void> => {};

export const setItemStatus = async (
  _boardId: number,
  _itemId: number,
  _status: string,
): Promise<void> => {};

export const deleteItem = async (
  _boardId: number,
  _itemId: number,
): Promise<void> => {};

export const clearCompleted = async (
  _boardId: number,
): Promise<{ cleared: number }> => {
  if (_mode === 'error') throw new Error('Mock error');
  return { cleared: 3 };
};

export const resetWeekly = async (
  _boardId: number,
): Promise<{ reset: number }> => {
  if (_mode === 'error') throw new Error('Mock error');
  return { reset: 5 };
};
