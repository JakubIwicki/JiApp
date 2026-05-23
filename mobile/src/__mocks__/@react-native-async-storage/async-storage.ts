const store = new Map<string, string>();

const AsyncStorage = {
  getItem(key: string): Promise<string | null> {
    return Promise.resolve(store.get(key) ?? null);
  },
  setItem(key: string, value: string): Promise<void> {
    store.set(key, value);
    return Promise.resolve();
  },
  removeItem(key: string): Promise<void> {
    store.delete(key);
    return Promise.resolve();
  },
  clear(): Promise<void> {
    store.clear();
    return Promise.resolve();
  },
};

export default AsyncStorage;
