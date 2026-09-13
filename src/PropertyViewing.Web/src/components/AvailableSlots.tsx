import type { ViewingSlot } from '../types/viewing';

type Props = {
  slots: ViewingSlot[];
  selectedStart: string | null;
  onSelect: (slot: ViewingSlot) => void;
  timeZoneId?: string; // Bổ sung timeZoneId để format chính xác giờ địa phương của nhà
};

const formatTime = (isoString: string, timeZoneId?: string) =>
  new Date(isoString).toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
    timeZone: timeZoneId || undefined, // Sử dụng timeZone của Property nếu có
  });

export function slotLabel(slot: ViewingSlot, timeZoneId?: string) {
  return `${formatTime(slot.startTime, timeZoneId)} - ${formatTime(slot.endTime, timeZoneId)}`;
}

export function AvailableSlots({ slots, selectedStart, onSelect, timeZoneId }: Props) {
  return (
    <div className="slot-grid">
      {slots.map((slot) => {
        const isSelected = selectedStart === slot.startTime;
        return (
          <button
            key={slot.startTime}
            type="button"
            className={isSelected ? 'slot selected' : 'slot'}
            onClick={() => onSelect(slot)}
          >
            {slotLabel(slot, timeZoneId)}
          </button>
        );
      })}
    </div>
  );
}