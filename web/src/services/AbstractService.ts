/**
 * Lean typed base for services that fetch JSON from HTTP endpoints.
 * Owns the fetch mechanics; subclasses own endpoint selection and
 * validation via the caller-supplied `parse` callback.
 */
export abstract class AbstractService {
  protected async getJson<T>(
    url: string,
    parse: (data: unknown) => T,
  ): Promise<T> {
    const res = await fetch(url);
    if (!res.ok) {
      throw new Error(`HTTP ${res.status}: ${res.statusText}`);
    }
    const json: unknown = await res.json();
    return parse(json);
  }
}
