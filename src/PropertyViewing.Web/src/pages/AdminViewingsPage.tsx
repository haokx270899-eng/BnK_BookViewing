import { useMemo, useState } from 'react'
import { ApiError } from '../api/viewingApi'
import { useAdminViewings, useProperties } from '../hooks/useViewings'
import type { AdminViewingFilter, AdminViewingItem } from '../types/viewing'

function formatTimeRange(startTimeUtc: string, endTimeUtc: string, timeZone: string): string {
  const formatter = new Intl.DateTimeFormat(undefined, {
    timeZone,
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  })
  return `${formatter.format(new Date(startTimeUtc))} - ${formatter.format(new Date(endTimeUtc))}`
}

function formatDateTime(utc: string, timeZone: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone,
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(utc))
}

function formatUtc(utc: string): string {
  return new Intl.DateTimeFormat(undefined, {
    timeZone: 'UTC',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
    timeZoneName: 'short',
  }).format(new Date(utc))
}

function ViewingRow({ viewing }: { viewing: AdminViewingItem }) {
  return (
    <tr>
      <td>#{viewing.id}</td>
      <td>
        <strong>{viewing.propertyAddress}</strong>
        <span className="table-subtext">Property #{viewing.propertyId}</span>
      </td>
      <td>
        <strong>{viewing.userName}</strong>
        <span className="table-subtext">{viewing.userEmail ?? 'No email address'}</span>
      </td>
      <td>
        <strong>{formatTimeRange(viewing.startTimeUtc, viewing.endTimeUtc, viewing.propertyTimeZoneId)}</strong>
        <span className="table-subtext">
          {formatDateTime(viewing.startTimeUtc, viewing.propertyTimeZoneId)} ({viewing.propertyTimeZoneId})
        </span>
      </td>
      <td><span className="utc-badge">{formatUtc(viewing.startTimeUtc)} – {formatUtc(viewing.endTimeUtc)}</span></td>
      <td>
        <span className="status-badge">Booked</span>
        <span className="table-subtext">Created {formatUtc(viewing.createdAt)}</span>
      </td>
    </tr>
  )
}

export function AdminViewingsPage() {
  const [propertyId, setPropertyId] = useState<number | null>(null)
  const [date, setDate] = useState('')
  const filters = useMemo<AdminViewingFilter>(() => ({ propertyId, date: date || null }), [propertyId, date])
  const properties = useProperties()
  const viewings = useAdminViewings(filters)

  const resetFilters = () => {
    setPropertyId(null)
    setDate('')
  }

  return (
    <main className="admin-page">
      <section className="card admin-card">
        <p className="eyebrow">Administration</p>
        <h1>Viewing dashboard</h1>
        <p className="intro">Review every booked property viewing in its property’s local time and UTC.</p>

        <section className="admin-filters" aria-label="Viewing filters">
          <label>
            Property
            <select value={propertyId ?? ''} disabled={properties.isLoading} onChange={(event) => setPropertyId(event.target.value ? Number(event.target.value) : null)}>
              <option value="">All properties</option>
              {(properties.data ?? []).map((property) => <option key={property.id} value={property.id}>{property.address}</option>)}
            </select>
          </label>
          <label>
            Viewing date
            <input type="date" value={date} onChange={(event) => setDate(event.target.value)} />
          </label>
          <button className="secondary-button" type="button" onClick={resetFilters} disabled={!propertyId && !date}>Reset filters</button>
        </section>

        {properties.isError && <p className="message error">Unable to load properties. You can still review all viewings.</p>}
        {viewings.isLoading && <p className="hint">Loading booked viewings...</p>}
        {viewings.isError && <p className="message error">{viewings.error instanceof ApiError ? viewings.error.message : 'Unable to load viewings. Please try again.'}</p>}
        {viewings.data && viewings.data.length === 0 && <p className="empty-state">No viewings found.</p>}
        {viewings.data && viewings.data.length > 0 && (
          <div className="table-scroll">
            <table className="viewings-table">
              <thead><tr><th>Viewing ID</th><th>Property address</th><th>User</th><th>Local viewing time</th><th>UTC time</th><th>Status / created at</th></tr></thead>
              <tbody>{viewings.data.map((viewing) => <ViewingRow key={viewing.id} viewing={viewing} />)}</tbody>
            </table>
          </div>
        )}
      </section>
    </main>
  )
}
