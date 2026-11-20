import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { RoomTypeOption } from '../../core/models/room';
import { ReservationsService } from './reservations.service';
import { RoomsService } from './rooms.service';

interface Paged<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

const FALLBACK_TYPES: RoomTypeOption[] = [
  { key: 'Sala', displayName: 'Sala' },
  { key: 'Escritorio', displayName: 'Escritorio' },
  { key: 'Auditorio', displayName: 'Auditorio' },
];

type DashboardReservation = {
  id: string;
  roomId: string;
  roomName: string;
  roomType: string;
  userEmail: string;
  startUtc: string;
  endUtc: string;
  isCancelled: boolean;
  cancelledAtUtc?: string | null;
  cancelledByUserId?: string | null;
};

type ChartRow = {
  typeKey: string;
  typeName: string;
  count: number;
  pct: number;
};

@Component({
  selector: 'app-member-dashboard-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  template: `
    <div class="stack">
      <div class="card stack">
        <div class="row" style="justify-content: space-between; gap: 1rem; flex-wrap: wrap">
          <div>
            <h2 style="margin: 0">Dashboard</h2>
            <p class="muted" style="margin: 0.25rem 0 0">
              Proximas reservas de los proximos 30 dias en horario UTC.
            </p>
          </div>
          <button class="btn" type="button" id="dashboard-reload-btn" (click)="reload()" [disabled]="loading()">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.5"/>
            </svg>
            Recargar
          </button>
        </div>

        <form class="filter-bar" [formGroup]="filters" (ngSubmit)="applyFilters()">
          <div class="field" style="min-width: 180px; flex: 1">
            <label for="date">Fecha (UTC)</label>
            <input id="date" type="date" formControlName="date" />
          </div>

          <div class="field" style="min-width: 220px; flex: 1">
            <label for="roomType">Tipo de espacio</label>
            <select id="roomType" formControlName="roomType">
              <option value="">Todos</option>
              @for (type of roomTypes(); track type.key) {
                <option [value]="type.key">{{ type.displayName }}</option>
              }
            </select>
          </div>

          <div class="filter-actions">
            <button class="btn" type="button" (click)="clearFilters()">Limpiar</button>
            <button class="btn btn-primary" type="submit">Aplicar</button>
          </div>
        </form>

        @if (loading()) {
          <div class="loading-state">
            <div class="skeleton" style="height: 220px; border-radius: 0.75rem"></div>
          </div>
        } @else if (error()) {
          <div class="error">{{ error() }}</div>
        } @else {
          <div class="inner-card stack">
            <div class="row" style="justify-content: space-between; gap: 1rem; flex-wrap: wrap">
              <strong style="font-size: 0.9rem">Reservas</strong>
              <span class="pill pill-info">{{ pageData().total }} total</span>
            </div>

            <table class="table">
              <thead>
                <tr>
                  <th>Fecha (UTC)</th>
                  <th>Usuario</th>
                  <th>Espacio</th>
                </tr>
              </thead>
              <tbody>
                @for (r of pageData().items; track r.id) {
                  <tr>
                    <td>{{ r.startUtc | date: 'short' : 'UTC' }}</td>
                    <td class="muted">{{ r.userEmail }}</td>
                    <td>
                      {{ r.roomName }}
                      <span class="pill" style="margin-left: 0.35rem">{{ r.roomType }}</span>
                    </td>
                  </tr>
                }
                @if (pageData().items.length === 0) {
                  <tr>
                    <td colspan="3" class="muted" style="text-align: center; padding: 2rem">Sin resultados.</td>
                  </tr>
                }
              </tbody>
            </table>

            <div class="row" style="justify-content: space-between; gap: 1rem; flex-wrap: wrap">
              <span class="muted">Pag. {{ pageData().page }} / {{ totalPages() }}</span>
              <div class="row">
                <button class="btn" type="button" (click)="prev()" [disabled]="pageData().page <= 1">Anterior</button>
                <button class="btn" type="button" (click)="next()" [disabled]="pageData().page >= totalPages()">Siguiente</button>
              </div>
            </div>
          </div>

          <div class="inner-card stack" style="margin-top: 1rem">
            <div class="row" style="justify-content: space-between; gap: 1rem; flex-wrap: wrap">
              <strong style="font-size: 0.9rem">Reservas por tipo</strong>
              <span class="pill pill-info">30 días</span>
            </div>

            @if (chartData().length === 0) {
              <p class="muted" style="margin: 1rem 0; text-align: center">Sin datos para graficar.</p>
            } @else {
              <div class="stack" style="gap: 0.85rem">
                @for (row of chartData(); track row.typeKey) {
                  <div class="stack" style="gap: 0.35rem">
                    <div class="row" style="justify-content: space-between; gap: 1rem">
                      <span style="font-size: 0.875rem">{{ row.typeName }}</span>
                      <span class="pill pill-info">{{ row.count }}</span>
                    </div>
                    <div class="bar">
                      <div class="bar-fill" [style.width.%]="row.pct"></div>
                    </div>
                  </div>
                }
              </div>
            }
          </div>
        }
      </div>
    </div>
  `,
  styles: [`
    .filter-bar {
      display: flex;
      gap: 0.75rem;
      align-items: flex-end;
      flex-wrap: wrap;
      padding: 1rem;
      background: var(--surface-2);
      border-radius: var(--radius-lg);
      border: 1px solid var(--border);
    }
    .filter-actions {
      display: flex;
      gap: 0.5rem;
      padding-top: 1.5rem;
    }
    .inner-card {
      background: var(--surface-2);
      border-radius: var(--radius-lg);
      padding: 1.25rem;
      border: 1px solid var(--border);
    }
    .loading-state {
      padding: 0.5rem 0;
    }
    .bar {
      width: 100%;
      height: 8px;
      border-radius: 999px;
      background: var(--surface-3);
      overflow: hidden;
    }
    .bar-fill {
      height: 100%;
      border-radius: 999px;
      background: linear-gradient(90deg, var(--indigo-600), var(--violet-500));
      transition: width 0.6s cubic-bezier(0.34, 1.56, 0.64, 1);
    }
  `],
})
export class MemberDashboardPageComponent {
  private readonly svc = inject(ReservationsService);
  private readonly roomsSvc = inject(RoomsService);
  private readonly fb = inject(FormBuilder);

  loading = signal(false);
  error = signal<string | null>(null);
  roomTypes = signal<RoomTypeOption[]>(FALLBACK_TYPES);
  chartCounts = signal<{ roomTypeKey: string; count: number }[]>([]);

  page = signal(1);
  pageSize = signal(10);
  raw = signal<Paged<DashboardReservation>>({ items: [], total: 0, page: 1, pageSize: 10 });

  filters = this.fb.nonNullable.group({
    date: [''],
    roomType: [''],
  });

  pageData = computed(() => this.raw());

  chartData = computed<ChartRow[]>(() => {
    const rows = this.chartCounts()
      .map((row) => {
        const type = this.roomTypes().find((x) => x.key === row.roomTypeKey);
        return {
          typeKey: row.roomTypeKey,
          typeName: type?.displayName ?? row.roomTypeKey,
          count: row.count,
        };
      })
      .sort((a, b) => b.count - a.count || a.typeName.localeCompare(b.typeName));

    const max = Math.max(1, ...rows.map((x) => x.count));
    return rows.map((x) => ({
      ...x,
      pct: Math.round((x.count / max) * 100),
    }));
  });

  totalPages = computed(() => {
    const total = this.pageData().total;
    return Math.max(1, Math.ceil(total / this.pageSize()));
  });

  constructor() {
    this.roomsSvc.listTypes().subscribe({
      next: (items) => {
        const list = (items ?? []).filter((x) => !!x?.key);
        if (list.length > 0) {
          this.roomTypes.set(list);
        }
      },
      error: () => {
        // Keep fallback list.
      },
    });

    this.reload();
  }

  reload() {
    this.loading.set(true);
    this.error.set(null);

    const { date, roomType } = this.filters.getRawValue();
    const query = {
      page: this.page(),
      pageSize: this.pageSize(),
      date,
      roomType,
    };

    forkJoin({
      list: this.svc.listUpcoming30Days(query),
      stats: this.svc.listUpcoming30DaysStats(query),
    }).subscribe({
      next: ({ list, stats }) => {
        this.loading.set(false);
        this.raw.set(normalizePaged<DashboardReservation>(list, this.page(), this.pageSize()));
        this.chartCounts.set(stats ?? []);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudieron cargar las reservas del dashboard.');
      },
    });
  }

  applyFilters() {
    this.page.set(1);
    this.reload();
  }

  clearFilters() {
    this.filters.reset({ date: '', roomType: '' });
    this.page.set(1);
    this.reload();
  }

  prev() {
    this.page.set(Math.max(1, this.page() - 1));
    this.reload();
  }

  next() {
    this.page.set(Math.min(this.totalPages(), this.page() + 1));
    this.reload();
  }
}

function normalizePaged<T>(res: any, page: number, pageSize: number): Paged<T> {
  if (Array.isArray(res)) {
    return {
      items: res as T[],
      total: (res as T[]).length,
      page: 1,
      pageSize: (res as T[]).length,
    };
  }

  if (res?.items && Array.isArray(res.items)) {
    return {
      items: res.items as T[],
      total: Number(res.total ?? res.items.length),
      page: Number(res.page ?? page),
      pageSize: Number(res.pageSize ?? pageSize),
    };
  }

  return { items: [], total: 0, page, pageSize };
}
