export type Property = { 
  id: number; 
  address: string; 
  timeZoneId: string; // Bổ sung trường này từ Backend
};

export type User = { 
  id: number; 
  name: string; 
  email: string | null; 
};

export type ViewingSlot = { 
  startTime: string; // Chuỗi ISO UTC: "2026-09-15T09:30:00Z"
  endTime: string; 
};

export type BookingRequest = { 
  propertyId: number; 
  userId: number; 
  startTime: string; 
};

export type BookingResult = BookingRequest & { 
  id: number; 
  endTime: string; 
};