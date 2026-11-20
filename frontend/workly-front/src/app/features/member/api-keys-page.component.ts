import { CommonModule, DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiKeyCreated, ApiKeySummary } from '../../core/models/api-key';
import { getHttpErrorMessage } from '../../core/utils/http-error';
import { ApiKeysService } from './api-keys.service';

@Component({
  selector: 'app-api-keys-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DatePipe],
  template: `
    <div class="stack">
      <div class="card stack">
        <div class="row" style="justify-content: space-between">
          <div>
            <h2 style="margin: 0">API Keys</h2>
            <p class="muted" style="margin: 0.25rem 0 0">
              La key solo se muestra una vez al crearla.
            </p>
          </div>
          <button class="btn" id="apikeys-reload-btn" type="button" (click)="reload()" [disabled]="loading()">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.5"/>
            </svg>
            Recargar
          </button>
        </div>

        @if (created()) {
          <div class="card stack" style="border-style: dashed">
            <div class="row" style="justify-content: space-between">
              <strong>API Key creada</strong>
              <span class="pill">solo visible ahora</span>
            </div>
            <div class="row" style="justify-content: space-between">
              <code style="word-break: break-all">{{ created()!.apiKey }}</code>
              <button class="btn" type="button" (click)="copy(created()!.apiKey)">Copiar</button>
            </div>
          </div>
        }

        @if (loading()) {
          <div class="skeleton" style="height:160px; border-radius:0.75rem"></div>
        } @else if (error()) {
          <div class="error">{{ error() }}</div>
        } @else {
          <table class="table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Creada</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (k of keys(); track k.id) {
                <tr>
                  <td>{{ k.name }}</td>
                  <td>{{ k.createdAt | date: 'short' }}</td>
                  <td>
                    @if (k.isActive) {
                      <span class="pill pill-success">Activa</span>
                    } @else {
                      <span class="pill pill-danger">Revocada</span>
                    }
                  </td>
                  <td style="text-align: right">
                    <button
                      class="btn btn-danger"
                      type="button"
                      [disabled]="!k.isActive || revokingId() === k.id"
                      (click)="revoke(k.id)"
                    >
                      @if (revokingId() === k.id) {
                        Revocando...
                      } @else {
                        Dar de baja
                      }
                    </button>
                  </td>
                </tr>
              }
              @if (keys().length === 0) {
                <tr>
                  <td colspan="4" class="muted">Sin API keys.</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>

      <div class="card stack">
        <h3 style="margin: 0">Crear API Key</h3>
        <form class="row" [formGroup]="form" (ngSubmit)="create()" style="align-items: flex-end">
          <div class="field" style="flex: 1">
            <label for="name">Nombre</label>
            <input id="name" formControlName="name" />
          </div>
          <button class="btn btn-primary" type="submit" [disabled]="form.invalid || creating()">
            @if (creating()) {
              Creando...
            } @else {
              Crear
            }
          </button>
        </form>

        @if (createError()) {
          <div class="error">{{ createError() }}</div>
        }
      </div>
    </div>
  `,
})
export class ApiKeysPageComponent {
  private readonly svc = inject(ApiKeysService);
  private readonly fb = inject(FormBuilder);

  private readonly storageKey = 'workly.apiKeys.local.v1';

  keys = signal<ApiKeySummary[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  creating = signal(false);
  createError = signal<string | null>(null);
  created = signal<ApiKeyCreated | null>(null);

  revokingId = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
  });

  constructor() {
    this.reload();
  }

  reload() {
    this.loading.set(true);
    this.error.set(null);
    try {
      const raw = localStorage.getItem(this.storageKey);
      const parsed = raw ? (JSON.parse(raw) as ApiKeySummary[]) : [];
      this.keys.set(Array.isArray(parsed) ? parsed : []);
    } catch {
      this.keys.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  create() {
    this.createError.set(null);
    this.created.set(null);
    if (this.form.invalid) return;

    const { name } = this.form.getRawValue();
    this.creating.set(true);
    this.svc.create(name).subscribe({
      next: (created) => {
        this.creating.set(false);
        this.created.set(created);
        this.form.reset();

        const nextSummary: ApiKeySummary = {
          id: created.id,
          name: created.name,
          createdAt: created.createdAt,
          isActive: true,
        };

        const next = [nextSummary, ...this.keys()];
        this.keys.set(next);
        this.persistKeys(next);
      },
      error: (err) => {
        this.creating.set(false);
        this.createError.set(getHttpErrorMessage(err) ?? 'No se pudo crear la API key.');
      },
    });
  }

  revoke(id: string) {
    this.revokingId.set(id);
    this.svc.revoke(id).subscribe({
      next: () => {
        this.revokingId.set(null);
        const next = this.keys().map((k) => (k.id === id ? { ...k, isActive: false } : k));
        this.keys.set(next);
        this.persistKeys(next);
      },
      error: (err) => {
        this.revokingId.set(null);
        this.error.set(getHttpErrorMessage(err) ?? 'No se pudo revocar la API key.');
      },
    });
  }

  private persistKeys(keys: ApiKeySummary[]) {
    try {
      localStorage.setItem(this.storageKey, JSON.stringify(keys));
    } catch {
      // ignore
    }
  }

  async copy(value: string) {
    try {
      await navigator.clipboard.writeText(value);
    } catch {
      // ignore
    }
  }
}
