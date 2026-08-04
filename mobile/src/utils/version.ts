export const isUpdateRequired = (installed: number, min: number): boolean =>
  installed < min;
