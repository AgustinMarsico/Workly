import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { getHttpErrorMessage } from '../../core/utils/http-error';
import { AuthInviteService } from './invite.service';

@Component({
  selector: 'app-invite-accept-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="invite-root">
      <div class="invite-shell">
        <div class="invite-hero">
          <p class="eyebrow">Workly</p>
          <h1>Aceptar invitación</h1>
          <p class="lead">Creá tu acceso para unirte al espacio de trabajo.</p>
        </div>

        <div class="invite-card card stack">
          @if (inviteLoading()) {
            <div class="notice notice-info">Validando invitación...</div>
          }

          @if (inviteError()) {
            <div class="notice notice-error">{{ inviteError() }}</div>
          }

          <div class="invite-summary">
            <span class="summary-label">Invitación para</span>
            <strong>{{ invitedEmail() || 'Cargando email...' }}</strong>
            <span class="summary-meta">{{ companyName() || 'Cargando empresa...' }}</span>
          </div>

          <form class="stack" [formGroup]="form" (ngSubmit)="submit()">
            <div class="field">
              <label for="password">Contraseña</label>
              <input
                id="password"
                type="password"
                formControlName="password"
                autocomplete="new-password"
                placeholder="Mínimo 8 caracteres"
              />
              @if (passwordControl.invalid && passwordControl.touched) {
                <small class="field-hint error">Ingresá una contraseña válida.</small>
              }
            </div>

            <div class="field">
              <label for="confirm">Confirmar contraseña</label>
              <input
                id="confirm"
                type="password"
                formControlName="confirm"
                autocomplete="new-password"
                placeholder="Repetí la contraseña"
              />
              @if (confirmControl.invalid && confirmControl.touched) {
                <small class="field-hint error">Confirmá tu contraseña.</small>
              }
            </div>

            @if (form.hasError('passwordMismatch') && confirmControl.touched) {
              <div class="notice notice-error">Las contraseñas no coinciden.</div>
            }

            @if (error()) {
              <div class="notice notice-error">{{ error() }}</div>
            }

            @if (success()) {
              <div class="notice notice-success">Cuenta creada. Redirigiendo a login...</div>
            }

            <div class="row actions">
              <a class="btn btn-secondary" routerLink="/login">Volver a login</a>
              <button
                class="btn btn-primary"
                type="submit"
                [disabled]="form.invalid || loading() || !!inviteError() || success()"
              >
                @if (loading()) {
                  Enviando...
                } @else {
                  Confirmar
                }
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .invite-root {
      min-height: 100vh;
      display: grid;
      place-items: center;
      padding: 2rem;
      background:
        radial-gradient(circle at top left, rgba(76, 29, 149, 0.18), transparent 36%),
        radial-gradient(circle at bottom right, rgba(30, 41, 59, 0.12), transparent 34%),
        linear-gradient(180deg, #f8fafc 0%, #eef2ff 100%);
    }
    .invite-shell {
      width: min(980px, 100%);
      display: grid;
      grid-template-columns: 1fr 1.1fr;
      gap: 1.5rem;
      align-items: stretch;
    }
    @media (max-width: 860px) {
      .invite-shell {
        grid-template-columns: 1fr;
      }
    }
    .invite-hero,
    .invite-card {
      border-radius: 24px;
      overflow: hidden;
    }
    .invite-hero {
      padding: 2rem;
      background: linear-gradient(145deg, #0f172a 0%, #312e81 55%, #7c3aed 100%);
      color: white;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      min-height: 320px;
      box-shadow: 0 24px 60px rgba(15, 23, 42, 0.18);
    }
    .eyebrow {
      margin: 0;
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.18em;
      opacity: 0.75;
    }
    .invite-hero h1 {
      margin: 0;
      font-size: clamp(2rem, 4vw, 3.25rem);
      line-height: 1.05;
      letter-spacing: -0.05em;
    }
    .lead {
      margin: 0;
      max-width: 28rem;
      color: rgba(255, 255, 255, 0.78);
      line-height: 1.6;
    }
    .invite-card {
      padding: 1.5rem;
      box-shadow: 0 18px 50px rgba(15, 23, 42, 0.12);
      background: rgba(255, 255, 255, 0.92);
      backdrop-filter: blur(10px);
      border: 1px solid rgba(148, 163, 184, 0.18);
    }
    .invite-summary {
      display: grid;
      gap: 0.25rem;
      padding: 1rem 1.1rem;
      border-radius: 18px;
      background: #f8fafc;
      border: 1px solid rgba(148, 163, 184, 0.22);
    }
    .summary-label {
      font-size: 0.78rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: #64748b;
    }
    .summary-meta {
      color: #475569;
      font-size: 0.95rem;
    }
    .mono {
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
      font-size: 0.8rem;
      word-break: break-all;
    }
    .notice {
      padding: 0.9rem 1rem;
      border-radius: 14px;
      border: 1px solid transparent;
      line-height: 1.45;
    }
    .notice-info {
      color: #1e3a8a;
      background: #eff6ff;
      border-color: #bfdbfe;
    }
    .notice-error {
      color: #991b1b;
      background: #fef2f2;
      border-color: #fecaca;
    }
    .notice-success {
      color: #14532d;
      background: #f0fdf4;
      border-color: #bbf7d0;
    }
    .field {
      display: grid;
      gap: 0.45rem;
    }
    .field-hint {
      font-size: 0.8rem;
    }
    .actions {
      display: flex;
      justify-content: space-between;
      gap: 0.75rem;
      margin-top: 0.25rem;
    }
  `],
})
export class InviteAcceptPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly inviteSvc = inject(AuthInviteService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  invitationId = computed(() => {
    return (
      this.route.snapshot.queryParamMap.get('invitationId') ??
      this.route.snapshot.paramMap.get('invitationId') ??
      ''
    );
  });

  invitationToken = computed(() => this.route.snapshot.queryParamMap.get('token') ?? '');

  inviteLoading = signal(true);
  inviteError = signal<string | null>(null);
  companyName = signal('');
  invitedEmail = signal('');

  loading = signal(false);
  error = signal<string | null>(null);
  success = signal(false);

  form = this.fb.nonNullable.group({
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirm: ['', [Validators.required]],
  }, { validators: (control) => this.passwordsMatchValidator(control) });

  constructor() {
    const invitationId = this.invitationId();
    const invitationToken = this.invitationToken();

    if (!invitationId) {
      this.inviteLoading.set(false);
      this.inviteError.set('ID de invitación inválido.');
      return;
    }

    if (!invitationToken) {
      this.inviteLoading.set(false);
      this.inviteError.set('Token de invitación inválido.');
      return;
    }

    this.inviteSvc.getInvite(invitationId).subscribe({
      next: (invite) => {
        this.inviteLoading.set(false);
        this.companyName.set(invite.companyName);
        this.invitedEmail.set(invite.email);
      },
      error: () => {
        this.inviteLoading.set(false);
        this.inviteError.set('La invitación no es válida o expiró.');
      },
    });
  }
  get passwordControl() {
    return this.form.controls.password;
  }

  get confirmControl() {
    return this.form.controls.confirm;
  }

  private passwordsMatchValidator(control: AbstractControl) {
    const password = control.get('password')?.value;
    const confirm = control.get('confirm')?.value;
    return password && confirm && password !== confirm ? { passwordMismatch: true } : null;
  }

  submit() {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const { password, confirm } = this.form.getRawValue();
    if (password !== confirm) {
      this.error.set('Las contraseñas no coinciden.');
      return;
    }

    this.loading.set(true);
    this.inviteSvc.acceptInvite(this.invitationId(), this.invitationToken(), password).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.success.set(true);
        const email = res.email ?? this.invitedEmail();
        void this.router.navigate(['/login'], { queryParams: { email } });
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(getHttpErrorMessage(err) ?? 'No se pudo aceptar la invitación.');
      },
    });
  }
}
