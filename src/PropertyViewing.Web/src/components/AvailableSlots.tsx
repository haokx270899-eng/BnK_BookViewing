import type { ViewingSlot } from '../types/viewing'

type Props = { slots: ViewingSlot[]; selectedStart: string | null; onSelect: (slot: ViewingSlot) => void }
const time = (value: string) => new Date(value).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false })
export function slotLabel(slot: ViewingSlot) { return `${time(slot.startTime)} - ${time(slot.endTime)}` }
export function AvailableSlots({ slots, selectedStart, onSelect }: Props) {
  return <div className="slot-grid">{slots.map(slot => <button key={slot.startTime} type="button" className={selectedStart === slot.startTime ? 'slot selected' : 'slot'} onClick={() => onSelect(slot)}>{slotLabel(slot)}</button>)}</div>
}
