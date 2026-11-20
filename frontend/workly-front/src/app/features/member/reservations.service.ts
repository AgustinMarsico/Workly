import { Injectable } from '@angular/core';
import { PagedResult } from '../../core/models/paged-result';
import { Reservation } from '../../core/models/reservation';
import { Area } from '../../core/models/area';
import { User } from '../../core/models/user';
import { ApiClient, IdentityApiClient } from '../../core/services/api-client.service';
import { ReservationApiClient } from '../../core/services/reservation-api-client.service';

export interface ListReservationsQuery {
  fromUtc?: string;
  toUtc?: string;
  roomType?: string;
  date?: string;
  page?: number;
  pageSize?: number;
}

export interface SearchReservationsQuery {
  roomId?: string;
  roomType?: string;
  userId?: string;
  areaId?: string;
  fromUtc?: string;
  toUtc?: string;
  status?: 'active' | 'cancelled' | 'all';
  page?: number;
  pageSize?: number;
}

export interface CreateReservationRequest {
  roomId: string;
  start: string;
  end: string;
}

export type UpdateReservationRequest = CreateReservationRequest;

export interface ReservationRoomTypeCount {
  roomTypeKey: string;
  count: number;
}

@Injectable({ providedIn: 'root' })
export class ReservationsService {
  constructor(
    private readonly api: ReservationApiClient,
    private readonly organizationApi: ApiClient,
    private readonly identityApi: IdentityApiClient,
  ) {}

  listUpcoming30Days(query: ListReservationsQuery) {
    return this.api.get<PagedResult<Reservation> | Reservation[]>('/api/reservations/upcoming', query);
  }

  listUpcoming30DaysStats(query: ListReservationsQuery) {
    return this.api.get<ReservationRoomTypeCount[]>('/api/reservations/upcoming/stats', query);
  }

  list(query: ListReservationsQuery) {
    return this.api.get<PagedResult<Reservation> | Reservation[]>('/api/reservations', query);
  }

  search(query: SearchReservationsQuery) {
    return this.api.get<PagedResult<Reservation>>('/api/reservations/search', query);
  }

  listCompanyAreas() {
    return this.organizationApi.get<Area[]>('/api/areas');
  }

  listCompanyUsers() {
    return this.identityApi.get<User[]>('/api/users/company');
  }

  create(req: CreateReservationRequest) {
    return this.api.post<Reservation>('/api/reservations', req);
  }

  update(id: string, req: UpdateReservationRequest) {
    return this.api.put<Reservation>(`/api/reservations/${encodeURIComponent(id)}`, req);
  }

  cancel(id: string) {
    return this.api.delete<void>(`/api/reservations/${encodeURIComponent(id)}`);
  }
}
