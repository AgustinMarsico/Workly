import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { HomeRedirectComponent } from './shared/components/home-redirect.component';
import { NotFoundComponent } from './shared/components/not-found.component';
import { LoginPageComponent } from './features/auth/login-page.component';
import { InviteAcceptPageComponent } from './features/auth/invite-accept-page.component';
import { AdminLayoutComponent } from './features/admin/admin-layout.component';
import { MemberLayoutComponent } from './features/member/member-layout.component';
import { AdminCompaniesPageComponent } from './features/admin/companies-page.component';
import { AdminInvitationsPageComponent } from './features/admin/invitations-page.component';
import { AdminUsersPageComponent } from './features/admin/users-page.component';
import { AdminAuditPageComponent } from './features/admin/audit-page.component';
import { MemberDashboardPageComponent } from './features/member/dashboard-page.component';
import { ApiKeysPageComponent } from './features/member/api-keys-page.component';
import { ReservationsPageComponent } from './features/member/reservations-page.component';
import { SpacesPageComponent } from './features/member/spaces-page.component';
import { NotificationPreferencesPageComponent } from './features/member/notification-preferences-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', component: HomeRedirectComponent },
  { path: 'login', component: LoginPageComponent },
  { path: 'invitations/accept', component: InviteAcceptPageComponent },
  { path: 'invitations/accept/:invitationId', component: InviteAcceptPageComponent },

  {
    path: 'admin',
    component: AdminLayoutComponent,
    canActivate: [authGuard, roleGuard('Admin')],
    children: [
      { path: '', pathMatch: 'full', component: AdminCompaniesPageComponent },
      { path: 'invitations', component: AdminInvitationsPageComponent },
      { path: 'users', component: AdminUsersPageComponent },
      { path: 'audit', component: AdminAuditPageComponent },
    ],
  },

  {
    path: 'app',
    component: MemberLayoutComponent,
    canActivate: [authGuard, roleGuard('Member')],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: MemberDashboardPageComponent },
      { path: 'reservations', component: ReservationsPageComponent },
      { path: 'api-keys', component: ApiKeysPageComponent },
      { path: 'spaces', component: SpacesPageComponent },
      { path: 'notifications', component: NotificationPreferencesPageComponent },
    ],
  },

  { path: '**', component: NotFoundComponent },
];
