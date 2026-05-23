const ReactNativeBlobUtil = {
  config: () => ({ fetch: () => Promise.resolve({ path: () => '' }) }),
  fs: { unlink: () => Promise.resolve() },
};

export default ReactNativeBlobUtil;
export { ReactNativeBlobUtil };
