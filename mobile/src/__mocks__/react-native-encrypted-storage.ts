const store = new Map<string, string>();

const EncryptedStorage = {
  setItem(key: string, value: string): Promise<void> {
    store.set(key, value);
    return Promise.resolve();
  },
  getItem(key: string): Promise<string | null> {
    return Promise.resolve(store.get(key) ?? null);
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

export default EncryptedStorage;
export { EncryptedStorage };
