import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="container">
      <div class="card stack" style="max-width: 520px; margin: 4rem auto">
        <h2 style="margin: 0">Página no encontrada</h2>
        <p class="muted" style="margin: 0">La ruta solicitada no existe.</p>
        <div class="row" style="justify-content: flex-end">
          <a class="btn btn-primary" routerLink="/">Ir al inicio</a>
        </div>
      </div>
    </div>
  `,
})
export class NotFoundComponent {}
