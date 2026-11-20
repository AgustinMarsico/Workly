import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Area } from '../../core/models/area';
import { Reservation } from '../../core/models/reservation';
import { Room, RoomTypeOption } from '../../core/models/room';
import { User } from '../../core/models/user';
import { getHttpErrorMessage } from '../../core/utils/http-error';
import { ReservationsService } from './reservations.service';
import { RoomsService } from './rooms.service';

@Component({
  selector: 'app-reservations-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  template: `
    <div class="stack">
      <div class="card stack">
        <div class="row" style="justify-content: space-between">
          <div>
            <h2 style="margin: 0">Reservas</h2>
            <p class="muted" style="margin: 0.25rem 0 0">Reservas de la empresa con filtros combinables.</p>
          </div>
          <button class="btn" id="reservations-reload-btn" type="button" (click)="reload()" [disabled]="loading()">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.5"/>
            </svg>
            Recargar
          </button>
        </div>

        <form class="stack" [formGroup]="filtersForm" (ngSubmit)="applyFilters()" style="gap: 0.75rem">
          <div class="grid-2">
            <div class="field">
              <label for="filterRoomId">Espacio físico</label>
              <select id="filterRoomId" formControlName="roomId">
                <option value="">Todos</option>
                @for (room of rooms(); track room.id) {
                  <option [value]="room.id">{{ room.name }} ({{ room.typeDisplayName }})</option>
                }
              </select>
            </div>
            <div class="field">
              <label for="filterRoomType">Tipo de espacio</label>
              <select id="filterRoomType" formControlName="roomType">
                <option value="">Todos</option>
                @for (type of roomTypes(); track type.key) {
                  <option [value]="type.key">{{ type.displayName }}</option>
                }
              </select>
            </div>
          </div>

          <div class="grid-2">
            <div class="field">
              <label for="filterUserId">Usuario</label>
              <select id="filterUserId" formControlName="userId">
                <option value="">Todos</option>
                @for (user of users(); track user.id) {
                  <option [value]="user.id">{{ user.email }}</option>
                }
              </select>
            </div>
            <div class="field">
              <label for="filterAreaId">Área</label>
              <select id="filterAreaId" formControlName="areaId">
                <option value="">Todas</option>
                @for (area of areas(); track area.id) {
                  <option [value]="area.id">{{ area.name }}</option>
                }
              </select>
            </div>
          </div>

          <div class="grid-2">
            <div class="field">
              <label for="filterFromDate">Desde</label>
              <input id="filterFromDate" type="date" formControlName="fromDate" />
            </div>
            <div class="field">
              <label for="filterToDate">Hasta</label>
              <input id="filterToDate" type="date" formControlName="toDate" />
            </div>
          </div>

          <div class="grid-2">
            <div class="field">
              <label for="filterStatus">Estado</label>
              <select id="filterStatus" formControlName="status">
                <option value="all">Todas</option>
                <option value="active">Activas</option>
                <option value="cancelled">Canceladas</option>
              </select>
            </div>
            <div class="field">
              <label for="filterPageSize">Tamaño de página</label>
              <input id="filterPageSize" type="number" min="1" max="100" formControlName="pageSize" />
            </div>
          </div>

          @if (searchError()) {
            <div class="error">{{ searchError() }}</div>
          }

          <div class="row" style="justify-content: space-between">
            <span class="pill pill-info">{{ total() }} resultado(s)</span>
            <div class="row" style="justify-content: flex-end">
              <button class="btn" type="button" (click)="clearFilters()" [disabled]="loading()">Limpiar</button>
              <button class="btn btn-primary" type="submit" [disabled]="loading() || filtersForm.invalid">Aplicar filtros</button>
            </div>
          </div>
        </form>

        @if (loading()) {
          <div class="skeleton" style="height:180px; border-radius:0.75rem"></div>
        } @else if (error()) {
          <div class="error">{{ error() }}</div>
        } @else {
          <table class="table">
            <thead>
              <tr>
                <th>Fecha</th>
                <th>Horario</th>
                <th>Espacio</th>
                <th>Usuario</th>
                <th>Área</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (r of reservations(); track r.id) {
                <tr>
                  <td>{{ r.startUtc | date: 'yyyy-MM-dd' : 'UTC' }}</td>
                  <td>{{ r.startUtc | date: 'HH:mm' : 'UTC' }} - {{ r.endUtc | date: 'HH:mm' : 'UTC' }}</td>
                  <td>
                    {{ r.roomName || 'Espacio eliminado' }}
                    @if (r.roomType) {
                      <span class="pill">{{ roomTypeName(r.roomType) }}</span>
                    }
                  </td>
                  <td class="muted">{{ r.userEmail || 'Usuario no disponible' }}</td>
                  <td class="muted">{{ areaName(r.userAreaId) }}</td>
                  <td>
                    @if (r.isCancelled) {
                      <span class="pill pill-danger">Cancelada</span>
                    } @else {
                      <span class="pill pill-success">Activa</span>
                    }
                  </td>
                  <td style="text-align: right">
                    <div class="row" style="justify-content: flex-end">
                      <button
                        class="btn"
                        type="button"
                        [disabled]="r.isCancelled || creating()"
                        (click)="startEdit(r)"
                      >
                        Editar
                      </button>
                      <button
                        class="btn btn-danger"
                        type="button"
                        [disabled]="r.isCancelled || cancellingId() === r.id"
                        (click)="cancel(r.id)"
                      >
                        @if (cancellingId() === r.id) {
                          Cancelando...
                        } @else {
                          Cancelar
                        }
                      </button>
                    </div>
                  </td>
                </tr>
              }
              @if (reservations().length === 0) {
                <tr>
                  <td colspan="7" class="muted" style="text-align:center; padding:2rem">No hay reservas para mostrar.</td>
                </tr>
              }
            </tbody>
          </table>

          <div class="row" style="justify-content: space-between">
            <span class="muted">Página {{ page() }} / {{ totalPages() }}</span>
            <div class="row" style="justify-content: flex-end">
              <button class="btn" type="button" (click)="previousPage()" [disabled]="loading() || page() <= 1">Anterior</button>
              <button class="btn" type="button" (click)="nextPage()" [disabled]="loading() || page() >= totalPages()">Siguiente</button>
            </div>
          </div>
        }
      </div>

      <div class="card stack">
        <div class="row" style="justify-content: space-between">
          <h3 style="margin: 0">
            @if (editingId()) {
              Editar reserva
            } @else {
              Crear reserva
            }
          </h3>
          @if (editingId()) {
            <button class="btn" type="button" (click)="cancelEdit()" [disabled]="creating()">Cancelar edición</button>
          }
        </div>

        <form class="stack" [formGroup]="form" (ngSubmit)="save()">
          <div class="grid-2">
            <div class="field">
              <label for="roomId">Espacio</label>
              <select id="roomId" formControlName="roomId">
                <option value="" disabled>Seleccionar...</option>
                @for (room of rooms(); track room.id) {
                  <option [value]="room.id">{{ room.name }} ({{ room.typeDisplayName }})</option>
                }
              </select>
            </div>
            <div class="field">
              <label for="date">Fecha (UTC)</label>
              <input id="date" type="date" formControlName="date" />
            </div>
          </div>

          <div class="grid-2">
            <div class="field">
              <label for="start">Hora inicio (UTC)</label>
              <input id="start" type="time" formControlName="start" />
            </div>
            <div class="field">
              <label for="end">Hora fin (UTC)</label>
              <input id="end" type="time" formControlName="end" />
            </div>
          </div>

          @if (createError()) {
            <div class="error">{{ createError() }}</div>
          }

          <div class="row" style="justify-content: flex-end">
            <button class="btn btn-primary" type="submit" [disabled]="form.invalid || creating()">
              @if (creating()) {
                Guardando...
              } @else if (editingId()) {
                Guardar cambios
              } @else {
                Crear
              }
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
})
export class ReservationsPageComponent {
  private readonly reservationsSvc = inject(ReservationsService);
  private readonly roomsSvc = inject(RoomsService);
  private readonly fb = inject(FormBuilder);

  rooms = signal<Room[]>([]);
  roomTypes = signal<RoomTypeOption[]>([]);
  users = signal<User[]>([]);
  areas = signal<Area[]>([]);
  reservations = signal<Reservation[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);
  searchError = signal<string | null>(null);
  total = signal(0);
  page = signal(1);
  pageSize = signal(10);

  totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));

  creating = signal(false);
  createError = signal<string | null>(null);
  editingId = signal<string | null>(null);

  cancellingId = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    roomId: ['', [Validators.required]],
    date: ['', [Validators.required]],
    start: ['', [Validators.required]],
    end: ['', [Validators.required]],
  });

  filtersForm = this.fb.nonNullable.group({
    roomId: [''],
    roomType: [''],
    userId: [''],
    areaId: [''],
    fromDate: [''],
    toDate: [''],
    status: ['all'],
    pageSize: [10, [Validators.min(1), Validators.max(100)]],
  });

  constructor() {
    this.loadFilterOptions();
    this.reload();
  }

  reload() {
    this.search(this.page());
  }

  applyFilters() {
    this.search(1);
  }

  previousPage() {
    if (this.page() <= 1) return;
    this.search(this.page() - 1);
  }

  nextPage() {
    if (this.page() >= this.totalPages()) return;
    this.search(this.page() + 1);
  }

  clearFilters() {
    this.filtersForm.reset({
      roomId: '',
      roomType: '',
      userId: '',
      areaId: '',
      fromDate: '',
      toDate: '',
      status: 'all',
      pageSize: 10,
    });
    this.search(1);
  }

  startEdit(reservation: Reservation) {
    if (reservation.isCancelled) return;

    const startParts = splitUtcDateTime(reservation.startUtc);
    const endParts = splitUtcDateTime(reservation.endUtc);
    if (!startParts || !endParts || startParts.date !== endParts.date) {
      this.error.set('No se pudo cargar la reserva para edición.');
      return;
    }

    this.editingId.set(reservation.id);
    this.createError.set(null);
    this.form.patchValue({
      roomId: reservation.roomId,
      date: startParts.date,
      start: startParts.time,
      end: endParts.time,
    });
  }

  cancelEdit() {
    this.editingId.set(null);
    this.createError.set(null);
    this.form.reset({
      roomId: '',
      date: '',
      start: '',
      end: '',
    });
  }

  save() {
    this.createError.set(null);
    if (this.form.invalid) return;

    const { roomId, date, start, end } = this.form.getRawValue();
    if (!date || !start || !end) {
      this.createError.set('Fecha u hora inválida.');
      return;
    }

    const startMinutes = toMinutes(start);
    const endMinutes = toMinutes(end);
    if (startMinutes === null || endMinutes === null) {
      this.createError.set('Fecha u hora inválida.');
      return;
    }

    if (endMinutes <= startMinutes) {
      this.createError.set('La hora fin debe ser posterior a la hora inicio.');
      return;
    }

    const startUtc = toUtcIso(date, start);
    const endUtc = toUtcIso(date, end);
    if (!startUtc || !endUtc) {
      this.createError.set('Fecha u hora inválida.');
      return;
    }

    this.creating.set(true);
    const id = this.editingId();
    const request = { roomId, start: startUtc, end: endUtc };
    const call$ = id
      ? this.reservationsSvc.update(id, request)
      : this.reservationsSvc.create(request);

    call$.subscribe({
      next: () => {
        this.creating.set(false);
        if (id) {
          this.cancelEdit();
          this.reload();
        } else {
          this.form.patchValue({ start: '', end: '' });
          this.search(1);
        }
      },
      error: (err) => {
        this.creating.set(false);
        this.createError.set(
          getHttpErrorMessage(err) ?? 'No se pudo guardar la reserva (posible superposición).',
        );
      },
    });
  }

  cancel(id: string) {
    this.cancellingId.set(id);
    this.reservationsSvc.cancel(id).subscribe({
      next: () => {
        this.cancellingId.set(null);
        this.reload();
      },
      error: (err) => {
        this.cancellingId.set(null);
        this.error.set(getHttpErrorMessage(err) ?? 'No se pudo cancelar la reserva.');
      },
    });
  }

  areaName(areaId: string | null | undefined) {
    if (!areaId) return 'Sin área';
    return this.areas().find((area) => area.id === areaId)?.name ?? 'Área no disponible';
  }

  roomTypeName(typeKey: string | null | undefined) {
    if (!typeKey) return '';
    return this.roomTypes().find((type) => type.key === typeKey)?.displayName ?? typeKey;
  }

  private search(nextPage: number) {
    this.searchError.set(null);
    this.error.set(null);
    if (this.filtersForm.invalid) return;

    const { roomId, roomType, userId, areaId, fromDate, toDate, status, pageSize } =
      this.filtersForm.getRawValue();

    const fromUtc = fromDate ? toUtcIso(fromDate, '00:00') : undefined;
    const toUtc = toDate ? toUtcIso(addOneDay(toDate), '00:00') : undefined;
    if ((fromDate && !fromUtc) || (toDate && !toUtc)) {
      this.searchError.set('Fecha inválida.');
      return;
    }

    if (fromUtc && toUtc && toUtc <= fromUtc) {
      this.searchError.set('La fecha hasta debe ser posterior a la fecha desde.');
      return;
    }

    const normalizedPageSize = clampNumber(pageSize, 1, 100);
    this.loading.set(true);
    this.reservationsSvc.search({
      roomId: emptyToUndefined(roomId),
      roomType: emptyToUndefined(roomType),
      userId: emptyToUndefined(userId),
      areaId: emptyToUndefined(areaId),
      fromUtc: fromUtc ?? undefined,
      toUtc: toUtc ?? undefined,
      status: normalizeStatus(status),
      page: nextPage,
      pageSize: normalizedPageSize,
    }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.reservations.set(res.items ?? []);
        this.total.set(Number(res.total ?? 0));
        this.page.set(Number(res.page ?? nextPage));
        this.pageSize.set(Number(res.pageSize ?? normalizedPageSize));
      },
      error: (err) => {
        this.loading.set(false);
        this.searchError.set(getHttpErrorMessage(err) ?? 'No se pudieron cargar las reservas.');
      },
    });
  }

  private loadFilterOptions() {
    this.roomsSvc.list().subscribe({
      next: (items) => {
        const rooms = items ?? [];
        this.rooms.set(rooms);
        if (this.roomTypes().length === 0) {
          this.roomTypes.set(uniqueRoomTypesFromRooms(rooms));
        }
      },
      error: () => this.rooms.set([]),
    });

    this.roomsSvc.listTypes().subscribe({
      next: (items) => this.roomTypes.set(items ?? []),
      error: () => this.roomTypes.set(uniqueRoomTypesFromRooms(this.rooms())),
    });

    this.reservationsSvc.listCompanyUsers().subscribe({
      next: (items) => this.users.set(items ?? []),
      error: () => this.users.set([]),
    });

    this.reservationsSvc.listCompanyAreas().subscribe({
      next: (items) => this.areas.set(items ?? []),
      error: () => this.areas.set([]),
    });
  }
}

function toUtcIso(date: string, time: string): string | null {
  if (!date || !time) return null;

  const dateMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(date);
  const timeMatch = /^(\d{2}):(\d{2})$/.exec(time);
  if (!dateMatch || !timeMatch) return null;

  const year = Number(dateMatch[1]);
  const month = Number(dateMatch[2]);
  const day = Number(dateMatch[3]);
  const hours = Number(timeMatch[1]);
  const minutes = Number(timeMatch[2]);

  const utcDate = new Date(Date.UTC(year, month - 1, day, hours, minutes, 0, 0));
  if (Number.isNaN(utcDate.getTime())) return null;

  return utcDate.toISOString();
}

function toMinutes(time: string): number | null {
  const match = /^(\d{2}):(\d{2})$/.exec(time);
  if (!match) return null;

  const hours = Number(match[1]);
  const minutes = Number(match[2]);
  if (!Number.isInteger(hours) || !Number.isInteger(minutes)) return null;
  if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) return null;

  return hours * 60 + minutes;
}

function splitUtcDateTime(value: string): { date: string; time: string } | null {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return null;

  const yyyy = date.getUTCFullYear().toString().padStart(4, '0');
  const mm = (date.getUTCMonth() + 1).toString().padStart(2, '0');
  const dd = date.getUTCDate().toString().padStart(2, '0');
  const hh = date.getUTCHours().toString().padStart(2, '0');
  const min = date.getUTCMinutes().toString().padStart(2, '0');

  return {
    date: `${yyyy}-${mm}-${dd}`,
    time: `${hh}:${min}`,
  };
}

function addOneDay(date: string): string {
  const parsed = toUtcIso(date, '00:00');
  if (!parsed) return date;
  const next = new Date(parsed);
  next.setUTCDate(next.getUTCDate() + 1);
  return next.toISOString().slice(0, 10);
}

function normalizeStatus(status: string): 'active' | 'cancelled' | 'all' {
  return status === 'active' || status === 'cancelled' ? status : 'all';
}

function clampNumber(value: number, min: number, max: number): number {
  const n = Number(value);
  if (!Number.isFinite(n)) return min;
  return Math.min(max, Math.max(min, Math.trunc(n)));
}

function emptyToUndefined(value: string | null | undefined): string | undefined {
  const trimmed = String(value ?? '').trim();
  return trimmed ? trimmed : undefined;
}

function uniqueRoomTypesFromRooms(rooms: Room[]): RoomTypeOption[] {
  const byKey = new Map<string, RoomTypeOption>();
  for (const room of rooms) {
    if (!room.typeKey || byKey.has(room.typeKey)) continue;
    byKey.set(room.typeKey, { key: room.typeKey, displayName: room.typeDisplayName || room.typeKey });
  }

  return [...byKey.values()].sort((a, b) => a.displayName.localeCompare(b.displayName));
}
