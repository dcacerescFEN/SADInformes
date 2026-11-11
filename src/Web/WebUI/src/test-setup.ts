/**
 * Angular unit tests run in a Node environment where the browser-like
 * localStorage may be unavailable. Provide an in-memory Storage implementation
 * so the existing persistence tests exercise the application behavior instead
 * of failing on the environment.
 */
class MemoryStorage implements Storage {
  private readonly values = new Map<string, string>();

  get length(): number {
    return this.values.size;
  }

  clear(): void {
    this.values.clear();
  }

  getItem(key: string): string | null {
    return this.values.get(String(key)) ?? null;
  }

  key(index: number): string | null {
    return [...this.values.keys()][index] ?? null;
  }

  removeItem(key: string): void {
    this.values.delete(String(key));
  }

  setItem(key: string, value: string): void {
    this.values.set(String(key), String(value));
  }
}

if (typeof globalThis.localStorage === 'undefined') {
  const storage = new MemoryStorage();
  Object.defineProperty(globalThis, 'Storage', {
    configurable: true,
    value: MemoryStorage,
  });
  Object.defineProperty(globalThis, 'localStorage', {
    configurable: true,
    value: storage,
  });
}
