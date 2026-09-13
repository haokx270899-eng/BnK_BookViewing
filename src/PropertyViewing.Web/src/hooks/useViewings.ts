import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { viewingApi } from '../api/viewingApi'
import type { AdminViewingFilter, BookingRequest } from '../types/viewing'

export function useProperties() { return useQuery({ queryKey: ['properties'], queryFn: viewingApi.getProperties }) }
export function useUsers() { return useQuery({ queryKey: ['users'], queryFn: viewingApi.getUsers }) }
export function useAvailableSlots(propertyId: number | null, date: string) {
  return useQuery({ queryKey: ['available-slots', propertyId, date], queryFn: () => viewingApi.getAvailableSlots(propertyId!, date), enabled: propertyId !== null && Boolean(date) })
}
export function useBookViewing() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: viewingApi.bookViewing,
    onSettled: async (_data, _error, booking: BookingRequest) => queryClient.invalidateQueries({ queryKey: ['available-slots', booking.propertyId] }),
  })
}

export function useAdminViewings(filters: AdminViewingFilter) {
  return useQuery({
    queryKey: ['admin-viewings', filters.propertyId ?? null, filters.date ?? null, filters.userId ?? null],
    queryFn: () => viewingApi.getAdminViewings(filters),
  })
}
