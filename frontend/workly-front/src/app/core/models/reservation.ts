export interface Reservation {
  id: string;
  roomId: string;
  roomName: string;
  roomType: string;
  userId: string;
  userEmail: string;
  userAreaId?: string | null;
  startUtc: string;
  endUtc: string;
  isCancelled: boolean;
  cancelledAtUtc?: string;
  cancelledByEmail?: string;
}
