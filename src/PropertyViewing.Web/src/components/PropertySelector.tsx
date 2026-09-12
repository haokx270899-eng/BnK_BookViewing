import type { Property } from '../types/viewing'

type Props = { properties: Property[]; value: number | null; onChange: (id: number | null) => void; disabled?: boolean }
export function PropertySelector({ properties, value, onChange, disabled }: Props) {
  return <label>Property<select value={value ?? ''} disabled={disabled} onChange={event => onChange(event.target.value ? Number(event.target.value) : null)}><option value="">Select a property</option>{properties.map(property => <option key={property.id} value={property.id}>{property.address}</option>)}</select></label>
}
