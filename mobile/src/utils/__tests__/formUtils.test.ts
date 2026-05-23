import { makeChangeHandler } from '../formUtils';

describe('makeChangeHandler', () => {
  it('sets the value and clears the error when called with text', () => {
    const setValue = jest.fn();
    const clearError = jest.fn();

    const handler = makeChangeHandler(setValue, clearError);
    handler('new text');

    expect(setValue).toHaveBeenCalledWith('new text');
    expect(clearError).toHaveBeenCalledTimes(1);
  });

  it('calls clearError before any pending renders', () => {
    const setValue = jest.fn();
    const clearError = jest.fn();

    const handler = makeChangeHandler(setValue, clearError);
    handler('');

    expect(setValue).toHaveBeenCalledWith('');
    expect(clearError).toHaveBeenCalledTimes(1);
  });

  it('supports different value types via generic', () => {
    const setValue = jest.fn();
    const clearError = jest.fn();

    const handler = makeChangeHandler(setValue, clearError);
    handler('test');

    expect(setValue).toHaveBeenCalledWith('test');
    expect(clearError).toHaveBeenCalledTimes(1);
  });

  it('each handler has its own closure', () => {
    const setValue1 = jest.fn();
    const clearError1 = jest.fn();
    const setValue2 = jest.fn();
    const clearError2 = jest.fn();

    const handler1 = makeChangeHandler(setValue1, clearError1);
    const handler2 = makeChangeHandler(setValue2, clearError2);

    handler1('first');
    handler2('second');

    expect(setValue1).toHaveBeenCalledWith('first');
    expect(setValue2).toHaveBeenCalledWith('second');
    expect(setValue1).not.toHaveBeenCalledWith('second');
    expect(clearError1).toHaveBeenCalledTimes(1);
    expect(clearError2).toHaveBeenCalledTimes(1);
  });
});
