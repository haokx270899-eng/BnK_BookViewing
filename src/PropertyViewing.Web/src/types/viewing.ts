export type Property = { id: number; address: string }
export type User = { id: number; name: string; email: string | null }
export type ViewingSlot = { startTime: string; endTime: string }
export type BookingRequest = { propertyId: number; userId: number; startTime: string }
export type BookingResult = BookingRequest & { id: number; endTime: string }
