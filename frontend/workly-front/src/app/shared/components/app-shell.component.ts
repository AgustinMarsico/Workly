import { CommonModule } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ConfirmDialogComponent } from './confirm-dialog.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet, ConfirmDialogComponent],
  template: `
    <div class="shell">
      <header class="header">
        <div class="container header-inner">
          <div class="brand">
            <div class="brand-icon">
              <svg
                width="18"
                height="18"
                viewBox="0 0 24 24"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
              >
                <rect x="3" y="3" width="8" height="8" rx="2" fill="currentColor" />
                <rect x="13" y="3" width="8" height="8" rx="2" fill="currentColor" opacity=".6" />
                <rect x="3" y="13" width="8" height="8" rx="2" fill="currentColor" opacity=".6" />
                <rect x="13" y="13" width="8" height="8" rx="2" fill="currentColor" opacity=".3" />
              </svg>
            </div>
            <span class="brand-name">Workly</span>
            @if (companyName()) {
              <span class="pill" style="font-size:0.7rem">{{ companyName() }}</span>
            }
          </div>

          <div class="header-right">
            @if (email()) {
              <span class="muted" style="font-size:0.825rem">{{ email() }}</span>
            }
            <button class="btn" id="logout-btn" type="button" (click)="logout()">
              <svg
                width="14"
                height="14"
                viewBox="0 0 24 24"
                fill="none"
                stroke="currentColor"
                stroke-width="2"
                stroke-linecap="round"
                stroke-linejoin="round"
              >
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                <polyline points="16 17 21 12 16 7" />
                <line x1="21" y1="12" x2="9" y2="12" />
              </svg>
              Salir
            </button>
          </div>
        </div>
      </header>

      <div class="container" style="padding-top: 1.5rem; padding-bottom: 2.5rem">
        <nav class="nav-bar">
          <div class="nav-links">
            @for (item of navItems(); track item.href) {
              <a
                class="nav-link"
                [routerLink]="item.href"
                routerLinkActive="active"
                [routerLinkActiveOptions]="{ exact: true }"
                >{{ item.label }}</a
              >
            }
          </div>
          @if (mode() === 'admin') {
            <span class="role-badge role-admin">Admin</span>
          } @else {
            <span class="role-badge role-member">Miembro</span>
          }
        </nav>

        <div class="page-content">
          <router-outlet />
        </div>
      </div>
      <app-confirm-dialog />
    </div>
  `,
  styles: [
    `
      .header {
        position: sticky;
        top: 0;
        z-index: 50;
        border-bottom: 1px solid rgba(255, 255, 255, 0.06);
        background: rgba(9, 9, 11, 0.82);
        backdrop-filter: blur(16px);
        -webkit-backdrop-filter: blur(16px);
      }
      .header-inner {
        display: flex;
        align-items: center;
        justify-content: space-between;
        height: 60px;
      }
      .brand {
        display: flex;
        align-items: center;
        gap: 0.6rem;
      }
      .brand-icon {
        width: 32px;
        height: 32px;
        border-radius: 8px;
        background: linear-gradient(135deg, #6366f1, #8b5cf6);
        display: flex;
        align-items: center;
        justify-content: center;
        color: white;
        flex-shrink: 0;
      }
      .brand-name {
        font-size: 1.05rem;
        font-weight: 700;
        letter-spacing: -0.03em;
        background: linear-gradient(90deg, #fff 30%, #a5b4fc);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
      }
      .header-right {
        display: flex;
        align-items: center;
        gap: 0.75rem;
      }
      .nav-bar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1.5rem;
        border-bottom: 1px solid rgba(255, 255, 255, 0.06);
        padding-bottom: 0.75rem;
      }
      .nav-links {
        display: flex;
        gap: 0.25rem;
      }
      .nav-link {
        text-decoration: none;
        color: var(--text-muted);
        font-size: 0.875rem;
        font-weight: 500;
        padding: 0.4rem 0.85rem;
        border-radius: var(--radius-md);
        border: 1px solid transparent;
        transition:
          color 0.18s,
          background 0.18s,
          border-color 0.18s;
      }
      .nav-link:hover {
        color: var(--text);
        background: rgba(255, 255, 255, 0.04);
        opacity: 1;
      }
      .nav-link.active {
        color: var(--indigo-400);
        background: rgba(99, 102, 241, 0.1);
        border-color: rgba(99, 102, 241, 0.25);
      }
      .role-badge {
        font-size: 0.7rem;
        font-weight: 600;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        padding: 0.2rem 0.6rem;
        border-radius: 999px;
        border: 1px solid;
      }
      .role-admin {
        background: rgba(139, 92, 246, 0.12);
        border-color: rgba(139, 92, 246, 0.3);
        color: #c4b5fd;
      }
      .role-member {
        background: rgba(99, 102, 241, 0.1);
        border-color: rgba(99, 102, 241, 0.3);
        color: #a5b4fc;
      }
      .page-content {
        animation: fadeIn 0.22s ease;
      }
      @keyframes fadeIn {
        from {
          opacity: 0;
          transform: translateY(6px);
        }
        to {
          opacity: 1;
          transform: translateY(0);
        }
      }
    `,
  ],
})
export class AppShellComponent {
  private readonly auth = inject(AuthService);

  mode = input.required<'admin' | 'member'>();

  email = computed(() => this.auth.session()?.email ?? '');
  companyName = computed(() => this.auth.session()?.companyName ?? '');

  navItems = computed(() => {
    if (this.mode() === 'admin') {
      return [
        { href: '/admin', label: 'Empresas' },
        { href: '/admin/invitations', label: 'Invitaciones' },
        { href: '/admin/users', label: 'Usuarios' },
        { href: '/admin/audit', label: 'Auditoría' },
      ];
    }

    return [
      { href: '/app/dashboard', label: 'Dashboard' },
      { href: '/app/reservations', label: 'Reservas' },
      { href: '/app/api-keys', label: 'API Keys' },
      { href: '/app/spaces', label: 'Espacios' },
      { href: '/app/notifications', label: 'Notificaciones' },
    ];
  });

  logout() {
    this.auth.logout();
  }
}
