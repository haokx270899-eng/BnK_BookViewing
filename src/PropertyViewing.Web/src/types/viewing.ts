export type Property = { 
  id: number; 
  address: string; 
  timeZoneId: string; 
};

export type User = { 
  id: number; 
  name: string; 
  email: string | null; 
};

export type ViewingSlot = { 
  localStartTime: string; // "2026-09-14T09:00:00" (Local time tại Property)
  localEndTime: string;   // "2026-09-14T09:30:00"
  utcStartTime: string;   // "2026-09-14T08:00:00Z" (ISO UTC string)
  utcEndTime: string;     // "2026-09-14T08:30:00Z"
};

export type BookingRequest = { 
  propertyId: number; 
  userId: number; 
  startTime: string; // Gửi chuỗi localStartTime (DateTimeKind.Unspecified) để Backend tự convert theo timezone của Property
};

export type BookingResult = {
  id: number;
  propertyId: number;
  userId: number;
  startTime: string; // ISO UTC string trả về từ Backend
  endTime: string;   // ISO UTC string trả về từ Backend
};

export type AdminViewingItem = {
  id: number;
  propertyId: number;
  propertyAddress: string;
  propertyTimeZoneId: string;
  userId: number;
  userName: string;
  userEmail: string | null;
  startTimeUtc: string;
  endTimeUtc: string;
  createdAt: string;
};

export type AdminViewingFilter = {
  propertyId?: number | null;
  date?: string | null;
  userId?: number | null;
};