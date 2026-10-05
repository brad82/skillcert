/** Error thrown for any non-2xx API response. `body` holds a ProblemDetails payload when the API sent one. */
export class ApiError extends Error {
  readonly status: number
  readonly body: unknown

  constructor(status: number, body: unknown) {
    super(`API request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.body = body
  }
}

/**
 * Fetch wrapper used by every Orval-generated call. The SPA and API share an origin
 * (Vite proxy locally, Caddy on the demo), so the auth cookie is sent automatically.
 */
export async function apiFetch<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, { ...init, credentials: 'same-origin' })
  const text = await response.text()
  const body: unknown = text ? JSON.parse(text) : undefined

  if (!response.ok) {
    throw new ApiError(response.status, body)
  }

  return body as T
}
