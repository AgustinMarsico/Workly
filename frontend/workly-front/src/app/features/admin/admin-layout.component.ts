import { Component } from '@angular/core';
import { AppShellComponent } from '../../shared/components/app-shell.component';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [AppShellComponent],
  template: `<app-shell mode="admin" />`,
})
export class AdminLayoutComponent {}
