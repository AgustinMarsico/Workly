import { Component } from '@angular/core';
import { AppShellComponent } from '../../shared/components/app-shell.component';

@Component({
  selector: 'app-member-layout',
  standalone: true,
  imports: [AppShellComponent],
  template: `<app-shell mode="member" />`,
})
export class MemberLayoutComponent {}
