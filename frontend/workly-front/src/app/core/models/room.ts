export interface Room {
  id: string;
  name: string;
  typeKey: string;
  typeDisplayName: string;
  maxCapacity: number;
  equipment?: string | null;
}

export interface RoomTypeOption {
  key: string;
  displayName: string;
}
