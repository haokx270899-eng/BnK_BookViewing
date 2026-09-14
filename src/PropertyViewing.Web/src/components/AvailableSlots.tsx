import type { ViewingSlot } from '../types/viewing';

type Props = {
  slots: ViewingSlot[];
  selectedStart: string | null;
  onSelect: (slot: ViewingSlot) => void;
};

// Cắt lấy 5 ký tự "HH:mm" trực tiếp từ chuỗi "YYYY-MM-DDTHH:mm:ss"
const formatLocalTime = (localIsoString: string) => {
  if (!localIsoString) return '';
  const timePart = localIsoString.split('T')[1]; // Lấy phần "HH:mm:ss"
  return timePart ? timePart.substring(0, 5) : localIsoString;
};

export function slotLabel(slot: ViewingSlot) {
  return `${formatLocalTime(slot.localStartTime)} - ${formatLocalTime(slot.localEndTime)}`;
}

export function AvailableSlots({ slots, selectedStart, onSelect }: Props) {
  return (
    <div className="slot-grid">
      {slots.map((slot) => {
        // So sánh theo localStartTime (hoặc utcStartTime tùy thuộc vào selectedStart của state cha)
        const isSelected = selectedStart === slot.localStartTime || selectedStart === slot.utcStartTime;
        
        return (
          <button
            key={slot.utcStartTime} // Dùng utcStartTime làm key để đảm bảo unique tuyệt đối
            type="button"
            className={isSelected ? 'slot selected' : 'slot'}
            onClick={() => onSelect(slot)}
          >
            {slotLabel(slot)}
          </button>
        );
      })}
    </div>
  );
}