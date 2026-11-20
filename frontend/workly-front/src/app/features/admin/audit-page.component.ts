import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AuditRecord } from '../../core/models/audit-record';
import { Company } from '../../core/models/company';
import { User } from '../../core/models/user';
import { getHttpErrorMessage } from '../../core/utils/http-error';
import { AdminService } from './admin.service';

const ACTION_TYPES = [
  'ClientCompanyCreated',
  'UserInvited',
  'UserRegistered',
  'UserAreaAssigned',
  'UserAreaModified',
  'PhysicalSpaceCreated',
  'PhysicalSpaceUpdated',
  'PhysicalSpaceDeleted',
  'ReservationCreated',
  'ReservationUpdated',
  'ReservationCancelled',
  'ApiKeyCreated',
  'ApiKeyRevoked',
  'SystemConfigurationChanged',
];

@Component({
  selector: 'app-admin-audit-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="stack">
      <div class="card stack">
        <div class="row" style="justify-content: space-between">
          <div>
            <h2 style="margin: 0">Auditoría</h2>
            <p class="muted" style="margin: 0.25rem 0 0">
              Registros inmutables de acciones administrativas y críticas.
            </p>
          </div>

          <button class="btn" type="button" (click)="reload()" [disabled]="loading()">
            Recargar
          </button>
        </div>

        <form class="filters" [formGroup]="filters" (ngSubmit)="applyFilters()">
          <div class="field">
            <label for="auditFrom">Desde</label>
            <input id="auditFrom" type="datetime-local" formControlName="from" />
          </div>

          <div class="field">
            <label for="auditTo">Hasta</label>
            <input id="auditTo" type="datetime-local" formControlName="to" />
          </div>

          <div class="field">
            <label for="auditUser">Usuario</label>
            <select id="auditUser" formControlName="userId">
              <option value="">Todos</option>
              @for (user of users(); track user.id) {
                <option [value]="user.id">{{ user.email }}</option>
              }
            </select>
          </div>

          <div class="field">
            <label for="auditAction">Acción</label>
            <select id="auditAction" formControlName="actionType">
              <option value="">Todas</option>
              @for (action of actionTypes; track action) {
                <option [value]="action">{{ actionLabel(action) }}</option>
              }
            </select>
          </div>

          <div class="field">
            <label for="auditCompany">Empresa</label>
            <select id="auditCompany" formControlName="companyId">
              <option value="">Todas</option>
              @for (company of companies(); track company.id) {
                <option [value]="company.id">{{ company.name }}</option>
              }
            </select>
          </div>

          <div class="filter-actions">
            <button class="btn btn-primary" type="submit" [disabled]="loading()">Filtrar</button>
            <button class="btn" type="button" (click)="clearFilters()" [disabled]="loading()">Limpiar</button>
          </div>
        </form>
      </div>

      <div class="card stack">
        <div class="row" style="justify-content: space-between">
          <span class="pill pill-info">{{ totalCount() }} registros</span>
          <span class="muted">Página {{ page() }} / {{ totalPages() }}</span>
        </div>

        @if (loading()) {
          <div class="skeleton" style="height: 220px; border-radius: 0.75rem"></div>
        } @else if (error()) {
          <div class="error">{{ error() }}</div>
        } @else {
          <div class="table-wrap">
            <table class="table audit-table">
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Usuario</th>
                  <th>Empresa</th>
                  <th>Acción</th>
                  <th>Entidad</th>
                  <th>Datos</th>
                </tr>
              </thead>
              <tbody>
                @for (record of records(); track record.id) {
                  <tr>
                    <td>{{ formatDate(record.occurredAt) }}</td>
                    <td>
                      <div>{{ record.userEmail || userEmail(record.userId) }}</div>
                      @if (record.userId) {
                        <code>{{ shortId(record.userId) }}</code>
                      }
                    </td>
                    <td>{{ companyName(record.companyId) }}</td>
                    <td><span class="pill">{{ actionLabel(record.actionType) }}</span></td>
                    <td>
                      <div>{{ record.entityType || 'Sin entidad' }}</div>
                      @if (record.entityId) {
                        <code>{{ shortId(record.entityId) }}</code>
                      }
                    </td>
                    <td>
                      <button class="btn" type="button" (click)="toggle(record.id)">
                        {{ expandedId() === record.id ? 'Ocultar' : 'Ver' }}
                      </button>
                    </td>
                  </tr>
                  @if (expandedId() === record.id) {
                    <tr class="detail-row">
                      <td colspan="6">
                        <div class="audit-detail">
                          <div>
                            <h3>Detalles</h3>
                            <pre>{{ prettyJson(record.detailsJson) }}</pre>
                          </div>
                          <div>
                            <h3>Valores anteriores</h3>
                            <pre>{{ prettyJson(record.oldValuesJson) }}</pre>
                          </div>
                          <div>
                            <h3>Valores nuevos</h3>
                            <pre>{{ prettyJson(record.newValuesJson) }}</pre>
                          </div>
                        </div>
                      </td>
                    </tr>
                  }
                }
                @if (records().length === 0) {
                  <tr>
                    <td colspan="6" class="muted" style="text-align:center; padding:2rem">
                      Sin registros para los filtros seleccionados.
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <div class="row" style="justify-content: flex-end">
            <button class="btn" type="button" (click)="previousPage()" [disabled]="page() <= 1">
              Anterior
            </button>
            <button class="btn" type="button" (click)="nextPage()" [disabled]="page() >= totalPages()">
              Siguiente
            </button>
          </div>
        }
      </div>
    </div>
  `,
  styles: [
    `
      .filters {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 1rem;
        align-items: end;
      }

      .filters .field {
        min-width: 0;
      }

      .filters input,
      .filters select {
        width: 100%;
      }

      .filter-actions {
        display: flex;
        gap: 0.5rem;
        align-items: center;
        justify-content: flex-end;
        min-width: 0;
      }

      .table-wrap {
        overflow-x: auto;
      }

      .audit-table {
        min-width: 920px;
      }

      .audit-detail {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 1rem;
        padding: 0.75rem 0;
      }

      .audit-detail h3 {
        margin-bottom: 0.5rem;
        font-size: 0.85rem;
        color: var(--text-muted);
      }

      .audit-detail pre {
        min-height: 96px;
        max-height: 260px;
        overflow: auto;
        white-space: pre-wrap;
        word-break: break-word;
        background: var(--surface-3);
        border: 1px solid var(--border);
        border-radius: var(--radius-md);
        padding: 0.75rem;
        color: var(--gray-300);
        font-size: 0.78rem;
      }

      .detail-row td {
        background: rgba(255, 255, 255, 0.02);
      }

      @media (max-width: 960px) {
        .filter-actions {
          grid-column: 1 / -1;
        }

        .audit-detail {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 640px) {
        .filters {
          grid-template-columns: 1fr;
        }
      }
    `,
  ],
})
export class AdminAuditPageComponent {
  private readonly admin = inject(AdminService);
  private readonly fb = inject(FormBuilder);

  actionTypes = ACTION_TYPES;
  records = signal<AuditRecord[]>([]);
  companies = signal<Company[]>([]);
  users = signal<User[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  expandedId = signal<string | null>(null);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize())));

  filters = this.fb.group({
    from: [''],
    to: [''],
    userId: [''],
    actionType: [''],
    companyId: [''],
  });

  constructor() {
    this.reload();
  }

  reload() {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      companies: this.admin.listCompanies(),
      users: this.admin.listUsers(),
      audit: this.admin.listAuditRecords({
        ...this.buildQuery(),
        page: this.page(),
        pageSize: this.pageSize(),
      }),
    }).subscribe({
      next: ({ companies, users, audit }) => {
        this.loading.set(false);
        this.companies.set(companies ?? []);
        this.users.set(users ?? []);
        this.records.set(audit.items ?? []);
        this.page.set(Number(audit.page ?? this.page()));
        this.pageSize.set(Number(audit.pageSize ?? this.pageSize()));
        this.totalCount.set(Number(audit.totalCount ?? 0));
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(getHttpErrorMessage(err) ?? 'No se pudieron cargar los registros de auditoría.');
      },
    });
  }

  applyFilters() {
    this.page.set(1);
    this.expandedId.set(null);
    this.reload();
  }

  clearFilters() {
    this.filters.reset({
      from: '',
      to: '',
      userId: '',
      actionType: '',
      companyId: '',
    });
    this.applyFilters();
  }

  previousPage() {
    if (this.page() <= 1) return;
    this.page.set(this.page() - 1);
    this.expandedId.set(null);
    this.reload();
  }

  nextPage() {
    if (this.page() >= this.totalPages()) return;
    this.page.set(this.page() + 1);
    this.expandedId.set(null);
    this.reload();
  }

  toggle(recordId: string) {
    this.expandedId.set(this.expandedId() === recordId ? null : recordId);
  }

  companyName(companyId?: string | null) {
    if (!companyId) return 'Sin empresa';
    return this.companies().find((company) => company.id === companyId)?.name ?? 'Empresa desconocida';
  }

  userEmail(userId?: string | null) {
    if (!userId) return 'Usuario desconocido';
    return this.users().find((user) => user.id === userId)?.email ?? 'Usuario desconocido';
  }

  actionLabel(actionType: string) {
    return actionType.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  shortId(value: string) {
    return value.length > 12 ? `${value.slice(0, 8)}...` : value;
  }

  formatDate(value: string) {
    return new Intl.DateTimeFormat('es-UY', {
      dateStyle: 'short',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  prettyJson(value?: string | null) {
    if (!value) return 'Sin datos';
    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }

  private buildQuery() {
    const raw = this.filters.getRawValue();
    return {
      from: toIso(raw.from),
      to: toIso(raw.to),
      userId: raw.userId,
      actionType: raw.actionType,
      companyId: raw.companyId,
    };
  }
}

function toIso(value?: string | null) {
  if (!value) return null;
  return new Date(value).toISOString();
}
