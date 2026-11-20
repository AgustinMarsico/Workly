import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from './admin.service';
import { Company } from '../../core/models/company';
import { getHttpErrorMessage } from '../../core/utils/http-error';

@Component({
  selector: 'app-admin-invitations-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="card stack">
      <div>
        <h2 style="margin: 0">Invitaciones</h2>
        <p class="muted" style="margin: 0">
          Creá invitaciones para usuarios miembros y asignales un área desde el alta.
        </p>
      </div>

      @if (loading()) {
        <p class="muted" style="margin: 0">Cargando empresas...</p>
      } @else if (error()) {
        <p class="error" style="margin: 0">{{ error() }}</p>
      }

      <form class="stack" [formGroup]="form" (ngSubmit)="submit()">
        <div class="grid-2">
          <div class="field">
            <label for="company">Empresa</label>
            <select id="company" formControlName="companyId" (change)="onCompanyChange()">
              <option value="" disabled>Seleccionar...</option>
              @for (c of companies(); track c.id) {
                <option [value]="c.id">{{ c.name }}</option>
              }
            </select>
          </div>

          <div class="field">
            <label for="email">Email del usuario miembro</label>
            <input id="email" type="email" formControlName="email" />
          </div>
        </div>

        <div class="grid-2">
          <div class="field">
            <label for="area">Área</label>
            <select id="area" formControlName="areaId" [disabled]="!selectedCompany() || selectedAreas().length === 0">
              <option value="" disabled>Seleccionar...</option>
              @for (a of selectedAreas(); track a.id) {
                <option [value]="a.id">{{ a.name }}</option>
              }
            </select>
            @if (selectedCompany() && selectedAreas().length === 0) {
              <span class="muted">La empresa seleccionada no tiene áreas cargadas.</span>
            }
          </div>
        </div>

        @if (success()) {
          <div class="pill" style="width: fit-content">Invitación creada</div>
        }
        @if (submitError()) {
          <div class="error">{{ submitError() }}</div>
        }

        <div class="row" style="justify-content: flex-end">
          <button class="btn btn-primary" type="submit" [disabled]="form.invalid || submitting()">
            @if (submitting()) {
              Creando...
            } @else {
              Crear invitación
            }
          </button>
        </div>
      </form>
    </div>
  `,
})
export class AdminInvitationsPageComponent {
  private readonly admin = inject(AdminService);
  private readonly fb = inject(FormBuilder);

  companies = signal<Company[]>([]);
  loading = signal(true);
  error = signal<string | null>(null);

  submitting = signal(false);
  submitError = signal<string | null>(null);
  success = signal(false);

  selectedCompany = () => this.companies().find((company) => company.id === this.form.controls.companyId.value) ?? null;
  selectedAreas = () => this.selectedCompany()?.areas ?? [];

  form = this.fb.nonNullable.group({
    companyId: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    areaId: ['', [Validators.required]],
  });

  constructor() {
    this.form.controls.companyId.valueChanges.subscribe(() => {
      this.form.controls.areaId.setValue('');
    });

    this.admin.listCompanies().subscribe({
      next: (items) => {
        this.loading.set(false);
        this.companies.set(items ?? []);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudieron cargar las empresas.');
      },
    });
  }

  onCompanyChange() {
    this.form.patchValue({ areaId: '' });
  }

  submit() {
    this.submitError.set(null);
    this.success.set(false);
    if (this.form.invalid) return;

    const { companyId, email, areaId } = this.form.getRawValue();
    this.submitting.set(true);
    this.admin.createInvitation({ userEmail: email, companyId, areaId }).subscribe({
      next: () => {
        this.submitting.set(false);
        this.success.set(true);
        this.form.patchValue({ email: '', areaId: '' });
        
        setTimeout(() => this.success.set(false), 3000);
      },
      error: (err) => {
        this.submitting.set(false);
        this.submitError.set(getHttpErrorMessage(err) ?? 'No se pudo crear la invitación.');
      },
    });
  }
}
