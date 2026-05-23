export const makeChangeHandler = (
  setValue: (text: string) => void,
  clearError: () => void,
): ((text: string) => void) => {
  return (text: string) => {
    setValue(text);
    clearError();
  };
};
