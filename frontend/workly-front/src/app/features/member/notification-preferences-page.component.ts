import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { getHttpErrorMessage } from '../../core/utils/http-error';
import { NotificationPreferencesService } from './notification-preferences.service';

@Component({
  selector: 'app-notification-preferences-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card stack">
      <div>
        <h2 style="margin: 0">Notificaciones</h2>
        <p class="muted" style="margin: 0.25rem 0 0">
          Preferencias de aviso para reservas.
        </p>
      </div>

      @if (loading()) {
        <div class="skeleton" style="height: 120px; border-radius: 0.75rem"></div>
      } @else if (error()) {
        <div class="error">{{ error() }}</div>
      } @else {
        <div class="preference-row">
          <div>
            <strong>Emails de reservas</strong>
            <p class="muted" style="margin: 0.25rem 0 0">
              Recibir avisos cuando se creen, modifiquen o cancelen reservas de tu área.
            </p>
          </div>

          <label class="switch" for="emailNotifications">
            <input
              id="emailNotifications"
              type="checkbox"
              [checked]="emailEnabled()"
              [disabled]="saving()"
              (change)="toggleEmailNotifications($event)"
            />
            <span class="slider"></span>
          </label>
        </div>

        @if (success()) {
          <div class="success-msg">{{ success() }}</div>
        }
      }
    </div>
  `,
  styles: [
    `
      .preference-row {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        padding: 1rem;
        background: var(--surface-2);
        border: 1px solid var(--border);
        border-radius: var(--radius-lg);
      }

      .switch {
        position: relative;
        display: inline-flex;
        width: 48px;
        height: 28px;
        flex: 0 0 auto;
        cursor: pointer;
      }

      .switch input {
        opacity: 0;
        width: 0;
        height: 0;
      }

      .slider {
        position: absolute;
        inset: 0;
        border-radius: 999px;
        background: var(--surface-3);
        border: 1px solid var(--border);
        transition: background 0.18s, border-color 0.18s;
      }

      .slider::before {
        content: '';
        position: absolute;
        width: 20px;
        height: 20px;
        left: 3px;
        top: 3px;
        border-radius: 50%;
        background: var(--gray-300);
        transition: transform 0.18s, background 0.18s;
      }

      input:checked + .slider {
        background: var(--indigo-600);
        border-color: var(--indigo-400);
      }

      input:checked + .slider::before {
        transform: translateX(20px);
        background: white;
      }

      input:disabled + .slider {
        opacity: 0.55;
        cursor: not-allowed;
      }
    `,
  ],
})
export class NotificationPreferencesPageComponent {
  private readonly preferences = inject(NotificationPreferencesService);

  loading = signal(true);
  saving = signal(false);
  emailEnabled = signal(true);
  error = signal<string | null>(null);
  success = signal<string | null>(null);

  constructor() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.preferences.get().subscribe({
      next: (value) => {
        this.loading.set(false);
        this.emailEnabled.set(value.emailNotificationsEnabled);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(getHttpErrorMessage(err) ?? 'No se pudieron cargar las preferencias.');
      },
    });
  }

  toggleEmailNotifications(event: Event) {
    const enabled = (event.target as HTMLInputElement).checked;
    const previous = this.emailEnabled();

    this.emailEnabled.set(enabled);
    this.saving.set(true);
    this.error.set(null);
    this.success.set(null);

    this.preferences.updateEmailNotifications(enabled).subscribe({
      next: (value) => {
        this.saving.set(false);
        this.emailEnabled.set(value.emailNotificationsEnabled);
        this.success.set(value.emailNotificationsEnabled ? 'Suscripción activada.' : 'Suscripción desactivada.');
      },
      error: (err) => {
        this.saving.set(false);
        this.emailEnabled.set(previous);
        this.error.set(getHttpErrorMessage(err) ?? 'No se pudo actualizar la preferencia.');
      },
    });
  }
}
