import type { AdminViewingFilter, AdminViewingItem, BookingRequest, BookingResult, Property, User, ViewingSlot } from '../types/viewing'

// Call HTTPS directly: a cross-origin HTTP-to-HTTPS redirect is rejected by browsers before CORS can succeed.
const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'https://localhost:54246'

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) { super(message) }
}

// Helper tính ngày kế tiếp (YYYY-MM-DD) cho param `to`
function getNextDay(dateString: string): string {
  const date = new Date(dateString)
  date.setDate(date.getDate() + 1)
  return date.toISOString().split('T')[0]
}

function toQueryString(filters: AdminViewingFilter): string {
  const parameters = new URLSearchParams()
  if (filters.propertyId != null) parameters.set('propertyId', String(filters.propertyId))
  if (filters.date) parameters.set('date', filters.date)
  if (filters.userId != null) parameters.set('userId', String(filters.userId))
  const query = parameters.toString()
  return query ? `?${query}` : ''
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  try {
    const response = await fetch(`${apiBaseUrl}${path}`, options)
    if (response.ok) return response.json() as Promise<T>
    
    const payload: unknown = await response.json().catch(() => null)
    
    // Lấy message từ 'detail' (ProblemDetails C#) hoặc 'message'
    let message = 'Unable to complete the request. Please try again.'
    if (typeof payload === 'object' && payload !== null) {
      if ('detail' in payload && typeof payload.detail === 'string') {
        message = payload.detail
      } else if ('message' in payload && typeof payload.message === 'string') {
        message = payload.message
      }
    }

    throw new ApiError(response.status, message)
  } catch (error) {
    if (error instanceof ApiError) throw error
    throw new ApiError(0, 'Unable to reach the server. Please try again.')
  }
}

export const viewingApi = {
  getProperties: () => request<Property[]>('/api/properties'),
  
  getUsers: () => request<User[]>('/api/users'),
  
  // Sửa param `to`: Tự động cộng 1 ngày để bao phủ trọn vẹn 24h của `date`
  getAvailableSlots: (propertyId: number, date: string) => 
    request<ViewingSlot[]>(`/api/viewings/available?propertyId=${propertyId}&from=${date}&to=${getNextDay(date)}`),
  
  bookViewing: (booking: BookingRequest) => 
    request<BookingResult>('/api/viewings', { 
      method: 'POST', 
      headers: { 'Content-Type': 'application/json' }, 
      body: JSON.stringify(booking) 
    }),
  getAdminViewings: (filters: AdminViewingFilter = {}) =>
    request<AdminViewingItem[]>(`/api/admin/viewings${toQueryString(filters)}`),
}
