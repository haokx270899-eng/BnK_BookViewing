import { BookingPage } from './pages/BookingPage'
import { AdminViewingsPage } from './pages/AdminViewingsPage'

export default function App() {
  return window.location.pathname === '/admin/viewings' ? <AdminViewingsPage /> : <BookingPage />
}
