export class ApiError extends Error {
  constructor(public status: number, public safeMessage: string) { super(safeMessage); }
}
export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const response = await fetch(path, { ...options, credentials: 'same-origin', cache: 'no-store' });
  if (!response.ok) {
    if (response.status === 401) window.dispatchEvent(new Event('gateway:session-ended'));
    throw new ApiError(response.status, response.status === 401 ? 'Your session ended. Sign in again.' :
      response.status === 403 ? 'You do not have access to this action.' : 'The service is unavailable. Retry or contact your operator.');
  }
  return response.json() as Promise<T>;
}
export async function mutate<T>(path: string, body: unknown, signal?: AbortSignal): Promise<T> {
  const { token } = await request<{ token: string }>('/auth/csrf', { signal });
  return request<T>(path, { method: 'POST', signal, headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token,
    'Idempotency-Key': crypto.randomUUID() }, body: JSON.stringify(body) });
}
