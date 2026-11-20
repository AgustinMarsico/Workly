import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { getHttpErrorMessage } from '../../core/utils/http-error';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="login-root">
      <!-- Left panel — branding -->
      <div class="login-brand" aria-hidden="true">
        <div class="brand-content">
          <div class="brand-logo">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <rect x="3" y="3" width="8" height="8" rx="2" fill="white"/>
              <rect x="13" y="3" width="8" height="8" rx="2" fill="white" opacity=".65"/>
              <rect x="3" y="13" width="8" height="8" rx="2" fill="white" opacity=".65"/>
              <rect x="13" y="13" width="8" height="8" rx="2" fill="white" opacity=".35"/>
            </svg>
            <span class="logo-text">Workly</span>
          </div>
          <div class="brand-copy">
            <h2 class="brand-headline">Tu oficina,<br>donde quieras.</h2>
            <p class="brand-sub">Reservá espacios, gestioná equipos y controlá tus API Keys desde un único lugar.</p>
          </div>
          <div class="brand-stats">
            <div class="stat">
              <span class="stat-num">∞</span>
              <span class="stat-label">Espacios</span>
            </div>
            <div class="stat-divider"></div>
            <div class="stat">
              <span class="stat-num">24/7</span>
              <span class="stat-label">Disponible</span>
            </div>
            <div class="stat-divider"></div>
            <div class="stat">
              <span class="stat-num">100%</span>
              <span class="stat-label">Seguro</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Right panel — form -->
      <div class="login-form-panel">
        <div class="login-form-wrap">
          <div class="form-header">
            <h1>Bienvenido</h1>
            <p class="muted">Ingresá con tu correo y contraseña para continuar.</p>
          </div>

          <form class="login-form" [formGroup]="form" (ngSubmit)="submit()">
            <div class="field">
              <label for="email">Correo electrónico</label>
              <div class="input-wrap">
                <svg class="input-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <rect x="2" y="4" width="20" height="16" rx="2"/>
                  <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>
                </svg>
                <input
                  id="email"
                  type="email"
                  formControlName="email"
                  autocomplete="email"
                  placeholder="nombre@empresa.com"
                  class="has-icon"
                />
              </div>
            </div>

            <div class="field">
              <label for="password">Contraseña</label>
              <div class="input-wrap">
                <svg class="input-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                  <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                </svg>
                <input
                  id="password"
                  type="password"
                  formControlName="password"
                  autocomplete="current-password"
                  placeholder="••••••••"
                  class="has-icon"
                />
              </div>
            </div>

            @if (error()) {
              <div class="error">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                {{ error() }}
              </div>
            }

            <button
              id="login-submit-btn"
              class="btn btn-primary login-btn"
              type="submit"
              [disabled]="form.invalid || loading()"
            >
              @if (loading()) {
                <span class="spinner"></span>
                Ingresando...
              } @else {
                Ingresar
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                  <line x1="5" y1="12" x2="19" y2="12"/>
                  <polyline points="12 5 19 12 12 19"/>
                </svg>
              }
            </button>
          </form>

          <p class="login-hint muted">
            ¿Recibiste una invitación? Usá el link del correo para acceder.
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-root {
      min-height: 100vh;
      display: grid;
      grid-template-columns: 1fr 1fr;
    }
    @media (max-width: 768px) {
      .login-root { grid-template-columns: 1fr; }
      .login-brand { display: none; }
    }

    /* ── Brand panel ── */
    .login-brand {
      background: linear-gradient(145deg, #1e1b4b 0%, #312e81 40%, #4c1d95 100%);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 3rem;
      position: relative;
      overflow: hidden;
    }
    .login-brand::before {
      content: '';
      position: absolute;
      inset: 0;
      background:
        radial-gradient(circle at 20% 80%, rgba(99,102,241,0.3) 0%, transparent 50%),
        radial-gradient(circle at 80% 20%, rgba(139,92,246,0.25) 0%, transparent 50%);
    }
    .brand-content {
      position: relative;
      z-index: 1;
      display: grid;
      gap: 2.5rem;
      max-width: 400px;
    }
    .brand-logo {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }
    .logo-text {
      font-size: 1.5rem;
      font-weight: 700;
      letter-spacing: -0.04em;
      color: white;
    }
    .brand-headline {
      font-size: 2.6rem;
      font-weight: 700;
      letter-spacing: -0.04em;
      line-height: 1.15;
      color: white;
    }
    .brand-sub {
      font-size: 1rem;
      color: rgba(255,255,255,0.65);
      line-height: 1.6;
      margin-top: 0.75rem;
    }
    .brand-stats {
      display: flex;
      align-items: center;
      gap: 1.5rem;
    }
    .stat { text-align: center; }
    .stat-num {
      display: block;
      font-size: 1.5rem;
      font-weight: 700;
      color: white;
      letter-spacing: -0.02em;
    }
    .stat-label {
      display: block;
      font-size: 0.75rem;
      color: rgba(255,255,255,0.5);
      text-transform: uppercase;
      letter-spacing: 0.06em;
      margin-top: 0.15rem;
    }
    .stat-divider {
      width: 1px;
      height: 36px;
      background: rgba(255,255,255,0.15);
    }

    /* ── Form panel ── */
    .login-form-panel {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem;
      background: var(--gray-950);
    }
    .login-form-wrap {
      width: 100%;
      max-width: 420px;
      display: grid;
      gap: 2rem;
    }
    .form-header { display: grid; gap: 0.4rem; }
    .form-header h1 { font-size: 1.875rem; }

    .login-form {
      display: grid;
      gap: 1.25rem;
    }

    /* Input with icon */
    .input-wrap {
      position: relative;
    }
    .input-icon {
      position: absolute;
      left: 0.85rem;
      top: 50%;
      transform: translateY(-50%);
      color: var(--gray-600);
      pointer-events: none;
    }
    input.has-icon {
      padding-left: 2.5rem;
    }

    .login-btn {
      width: 100%;
      justify-content: center;
      padding: 0.7rem 1.25rem;
      font-size: 0.9375rem;
      margin-top: 0.25rem;
    }

    /* Spinner */
    @keyframes spin {
      to { transform: rotate(360deg); }
    }
    .spinner {
      width: 15px;
      height: 15px;
      border: 2px solid rgba(255,255,255,0.25);
      border-top-color: white;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }

    .login-hint {
      text-align: center;
      font-size: 0.825rem;
    }
  `],
})
export class LoginPageComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  loading = signal(false);
  error = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  constructor() {
    const email = this.route.snapshot.queryParamMap.get('email');
    if (email) this.form.patchValue({ email });

    if (this.auth.isAuthenticated()) {
      void this.router.navigateByUrl(this.auth.routeAfterLogin(this.auth.role()));
    }
  }

  submit() {
    this.error.set(null);
    if (this.form.invalid) return;

    const { email, password } = this.form.getRawValue();
    this.loading.set(true);

    this.auth.login(email, password).subscribe({
      next: (session) => {
        this.loading.set(false);
        void this.router.navigateByUrl(this.auth.routeAfterLogin(session.role));
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(getHttpErrorMessage(err) ?? 'No se pudo iniciar sesión.');
      },
    });
  }
}
