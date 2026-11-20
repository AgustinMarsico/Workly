import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { AdminService } from './admin.service';
import { Company } from '../../core/models/company';
import { User } from '../../core/models/user';
import { getHttpErrorMessage } from '../../core/utils/http-error';

@Component({
  selector: 'app-admin-users-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card stack">
      <div class="row" style="justify-content: space-between">
        <div>
          <h2 style="margin: 0">Usuarios</h2>
          <p class="muted" style="margin: 0.25rem 0 0">
            Vista global de usuarios con su empresa y área.
          </p>
        </div>

        <button class="btn" type="button" (click)="reload()" [disabled]="loading()">
          Recargar
        </button>
      </div>

      @if (loading()) {
        <div class="skeleton" style="height: 180px; border-radius: 0.75rem"></div>
      } @else if (error()) {
        <div class="error">{{ error() }}</div>
      } @else {
        <table class="table">
          <thead>
            <tr>
              <th>Email</th>
              <th>Empresa</th>
              <th>Área</th>
              <th>Rol</th>
            </tr>
          </thead>
          <tbody>
            @for (user of users(); track user.id) {
              <tr>
                <td>{{ user.email }}</td>
                <td>{{ companyName(user.companyId) }}</td>
                <td class="muted">{{ areaName(user.companyId, user.areaId) }}</td>
                <td>{{ user.role }}</td>
              </tr>
            }
            @if (users().length === 0) {
              <tr>
                <td colspan="4" class="muted" style="text-align:center; padding:2rem">Sin usuarios registrados.</td>
              </tr>
            }
          </tbody>
        </table>
      }
    </div>
  `,
})
export class AdminUsersPageComponent {
  private readonly admin = inject(AdminService);

  users = signal<User[]>([]);
  companies = signal<Company[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor() {
    this.reload();
  }

  reload() {
    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      companies: this.admin.listCompanies(),
      users: this.admin.listUsers(),
    }).subscribe({
      next: ({ companies, users }) => {
        this.loading.set(false);
        this.companies.set(companies ?? []);
        this.users.set(users ?? []);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(getHttpErrorMessage(err) ?? 'No se pudieron cargar los usuarios.');
      },
    });
  }

  companyName(companyId?: string | null) {
    if (!companyId) return 'Sin empresa';
    return this.companies().find((company) => company.id === companyId)?.name ?? 'Empresa desconocida';
  }

  areaName(companyId?: string | null, areaId?: string | null) {
    if (!companyId || !areaId) return 'Sin área';
    const company = this.companies().find((item) => item.id === companyId);
    return company?.areas.find((area) => area.id === areaId)?.name ?? 'Área desconocida';
  }
}