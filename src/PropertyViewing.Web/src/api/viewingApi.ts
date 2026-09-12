import type { BookingRequest, BookingResult, Property, User, ViewingSlot } from '../types/viewing'

// Call HTTPS directly: a cross-origin HTTP-to-HTTPS redirect is rejected by browsers before CORS can succeed.
const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'https://localhost:54246'

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) { super(message) }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  try {
    const response = await fetch(`${apiBaseUrl}${path}`, options)
    if (response.ok) return response.json() as Promise<T>
    const payload: unknown = await response.json().catch(() => null)
    const message = typeof payload === 'object' && payload !== null && 'message' in payload && typeof payload.message === 'string'
      ? payload.message : 'Unable to complete the request. Please try again.'
    throw new ApiError(response.status, message)
  } catch (error) {
    if (error instanceof ApiError) throw error
    throw new ApiError(0, 'Unable to reach the server. Please try again.')
  }
}

export const viewingApi = {
  getProperties: () => request<Property[]>('/api/properties'),
  getUsers: () => request<User[]>('/api/users'),
  getAvailableSlots: (propertyId: number, date: string) => request<ViewingSlot[]>(`/api/viewings/available?propertyId=${propertyId}&from=${date}&to=${date}`),
  bookViewing: (booking: BookingRequest) => request<BookingResult>('/api/viewings', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(booking) }),
}
