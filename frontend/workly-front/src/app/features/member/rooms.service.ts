import { Injectable } from '@angular/core';
import { RoomsApiClient } from '../../core/services/api-client.service';
import { Room, RoomTypeOption } from '../../core/models/room';

export interface UpsertRoomRequest {
  name: string;
  typeKey: string;
  maxCapacity: number;
  equipment?: string | null;
}

@Injectable({ providedIn: 'root' })
export class RoomsService {
  constructor(private readonly api: RoomsApiClient) {}

  list() {
    return this.api.get<Room[]>('/api/rooms');
  }

  listTypes() {
    return this.api.get<RoomTypeOption[]>('/api/rooms/types');
  }

  create(req: UpsertRoomRequest) {
    return this.api.post<Room>('/api/rooms', req);
  }

  update(id: string, req: UpsertRoomRequest) {
    return this.api.put<Room>(`/api/rooms/${encodeURIComponent(id)}`, req);
  }

  delete(id: string) {
    return this.api.delete<void>(`/api/rooms/${encodeURIComponent(id)}`);
  }
}
