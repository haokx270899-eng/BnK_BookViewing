import { useEffect, useState } from "react";
import { ApiError } from "../api/viewingApi";
import { AvailableSlots, slotLabel } from "../components/AvailableSlots";
import { BookingForm } from "../components/BookingForm";
import { DateSelector } from "../components/DateSelector";
import { PropertySelector } from "../components/PropertySelector";
import {
  useAvailableSlots,
  useBookViewing,
  useProperties,
  useUsers,
} from "../hooks/useViewings";
import type { ViewingSlot } from "../types/viewing";

const getTodayString = () => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
};

export function BookingPage() {
  const [propertyId, setPropertyId] = useState<number | null>(null);
  const [date, setDate] = useState(getTodayString());
  const [selectedSlot, setSelectedSlot] = useState<ViewingSlot | null>(null);
  const [userId, setUserId] = useState<number | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const properties = useProperties();
  const users = useUsers();
  const slots = useAvailableSlots(propertyId, date);
  const booking = useBookViewing();

  const selectedProperty = properties.data?.find((p) => p.id === propertyId);

  useEffect(() => {
    setSelectedSlot(null);
    setMessage(null);
  }, [propertyId, date]);

  // Cập nhật so sánh theo utcStartTime
  useEffect(() => {
    if (
      selectedSlot &&
      !slots.data?.some((slot) => slot.utcStartTime === selectedSlot.utcStartTime)
    ) {
      setSelectedSlot(null);
    }
  }, [slots.data, selectedSlot]);

  const handleBook = () => {
    if (!propertyId || !userId || !selectedSlot) return;
    setMessage(null);

    // Gửi localStartTime sang Backend để quy đổi đúng theo múi giờ của Property
    booking.mutate(
      { propertyId, userId, startTime: selectedSlot.localStartTime },
      {
        onSuccess: () => {
          setMessage("Viewing booked successfully.");
          setSelectedSlot(null);
        },
        onError: (error) =>
          setMessage(
            error instanceof ApiError && error.status === 409
              ? "This slot has just been booked by another user. Please select another slot."
              : error instanceof ApiError &&
                  (error.status === 400 || error.status === 404)
                ? error.message
                : "Unable to complete the booking. Please try again.",
          ),
      },
    );
  };

  const lookupError = properties.isError || users.isError;

  return (
    <main>
      <section className="card">
        <p className="eyebrow">Property viewing</p>
        <h1>Book a viewing</h1>
        <p className="intro">
          Choose a property, date and available 30-minute slot.
        </p>
        {lookupError && (
          <p className="message error">
            Unable to load properties or users. Refresh and try again.
          </p>
        )}
        <div className="selectors">
          <PropertySelector
            properties={properties.data ?? []}
            value={propertyId}
            onChange={setPropertyId}
            disabled={properties.isLoading}
          />
          <DateSelector value={date} onChange={setDate} />
        </div>

        {selectedProperty && (
          <p className="hint">
            📍 Property Time Zone:{" "}
            <strong>{selectedProperty.timeZoneId}</strong>
          </p>
        )}

        <section className="slots">
          <h2>Available viewing slots</h2>
          {!propertyId && (
            <p className="hint">Select a property to find available slots.</p>
          )}
          {slots.isLoading && (
            <p className="hint">Loading available slots...</p>
          )}
          {slots.isError && (
            <p className="message error">
              Unable to load available slots. Please try again.
            </p>
          )}
          {slots.data && slots.data.length === 0 && (
            <p className="hint">No available viewing slots for this date.</p>
          )}
          {slots.data && slots.data.length > 0 && (
            <AvailableSlots
              slots={slots.data}
              selectedStart={selectedSlot?.localStartTime ?? null}
              onSelect={setSelectedSlot}
            />
          )}
          {selectedSlot && (
            <p className="selected-slot">
              Selected slot:{" "}
              <strong>
                {slotLabel(selectedSlot)}
              </strong>
            </p>
          )}
        </section>
        <BookingForm
          users={users.data ?? []}
          userId={userId}
          onUserChange={setUserId}
          onBook={handleBook}
          booking={booking.isPending}
          disabled={
            !propertyId || !userId || !selectedSlot || booking.isPending
          }
        />
        {message && (
          <p
            className={
              message.startsWith("Viewing")
                ? "message success"
                : "message error"
            }
            role="status"
          >
            {message}
          </p>
        )}
      </section>
    </main>
  );
}