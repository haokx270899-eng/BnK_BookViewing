import type { User } from '../types/viewing'

type Props = { users: User[]; userId: number | null; onUserChange: (id: number | null) => void; onBook: () => void; disabled: boolean; booking: boolean }
export function BookingForm({ users, userId, onUserChange, onBook, disabled, booking }: Props) {
  return <div className="booking-form"><label>User<select value={userId ?? ''} onChange={event => onUserChange(event.target.value ? Number(event.target.value) : null)}><option value="">Select a user</option>{users.map(user => <option key={user.id} value={user.id}>{user.name}</option>)}</select></label><button className="book-button" type="button" onClick={onBook} disabled={disabled}>{booking ? 'Booking viewing...' : 'Book viewing'}</button></div>
}
