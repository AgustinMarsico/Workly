import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Room, RoomTypeOption } from '../../core/models/room';
import { getHttpErrorMessage } from '../../core/utils/http-error';
import { RoomsService } from './rooms.service';

const FALLBACK_TYPES: RoomTypeOption[] = [
  { key: 'Sala', displayName: 'Sala' },
  { key: 'Escritorio', displayName: 'Escritorio' },
  { key: 'Auditorio', displayName: 'Auditorio' },
];

@Component({
  selector: 'app-spaces-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="stack">
      <div class="card stack">
        <div class="row" style="justify-content: space-between">
          <div>
            <h2 style="margin: 0">Espacios físicos</h2>
            <p class="muted" style="margin: 0.25rem 0 0">
              Gestioná salas, escritorios y auditorios.
            </p>
          </div>
          <button class="btn" id="spaces-reload-btn" type="button" (click)="reload()" [disabled]="loading()">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="1 4 1 10 7 10"/><path d="M3.51 15a9 9 0 1 0 .49-4.5"/>
            </svg>
            Recargar
          </button>
        </div>

        @if (loading()) {
          <div class="skeleton" style="height:160px; border-radius:0.75rem"></div>
        } @else if (error()) {
          <div class="error">{{ error() }}</div>
        } @else {
          <table class="table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Tipo</th>
                <th>Capacidad</th>
                <th>Equipamiento</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (r of rooms(); track r.id) {
                <tr>
                  <td>{{ r.name }}</td>
                  <td>{{ r.typeDisplayName }}</td>
                  <td>{{ r.maxCapacity }}</td>
                  <td class="muted">{{ r.equipment ?? '' }}</td>
                  <td style="text-align: right">
                    <div class="row" style="justify-content: flex-end">
                      <button class="btn" type="button" (click)="startEdit(r)">Editar</button>
                      <button
                        class="btn btn-danger"
                        type="button"
                        (click)="remove(r.id)"
                        [disabled]="deletingId() === r.id"
                      >
                        @if (deletingId() === r.id) {
                          Eliminando...
                        } @else {
                          Eliminar
                        }
                      </button>
                    </div>
                  </td>
                </tr>
              }
              @if (rooms().length === 0) {
                <tr>
                  <td colspan="5" class="muted" style="text-align:center; padding:2rem">Sin espacios registrados.</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>

      <div class="card stack">
        <div class="row" style="justify-content: space-between">
          <h3 style="margin: 0">
            @if (editingId()) {
              Editar espacio
            } @else {
              Crear espacio
            }
          </h3>
          @if (editingId()) {
            <button class="btn" type="button" (click)="cancelEdit()">Cancelar edición</button>
          }
        </div>

        <form class="stack" [formGroup]="form" (ngSubmit)="save()">
          <div class="grid-2">
            <div class="field">
              <label for="name">Nombre</label>
              <input id="name" formControlName="name" />
            </div>

            <div class="field">
              <label for="type">Tipo</label>
              <select id="type" formControlName="typeKey">
                @for (t of types(); track t.key) {
                  <option [value]="t.key">{{ t.displayName }}</option>
                }
              </select>
            </div>
          </div>

          <div class="grid-2">
            <div class="field">
              <label for="maxCapacity">Capacidad máxima</label>
              <input id="maxCapacity" type="number" formControlName="maxCapacity" />
            </div>
            <div class="field">
              <label for="equipment">Equipamiento</label>
              <input id="equipment" formControlName="equipment" />
            </div>
          </div>

          @if (saveError()) {
            <div class="error">{{ saveError() }}</div>
          }

          <div class="row" style="justify-content: flex-end">
            <button class="btn btn-primary" type="submit" [disabled]="form.invalid || saving()">
              @if (saving()) {
                Guardando...
              } @else {
                Guardar
              }
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
})
export class SpacesPageComponent {
  private readonly svc = inject(RoomsService);
  private readonly fb = inject(FormBuilder);

  rooms = signal<Room[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  types = signal<RoomTypeOption[]>(FALLBACK_TYPES);

  editingId = signal<string | null>(null);
  saving = signal(false);
  saveError = signal<string | null>(null);
  deletingId = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    typeKey: [FALLBACK_TYPES[0].key, [Validators.required]],
    maxCapacity: [1, [Validators.required, Validators.min(1)]],
    equipment: [''],
  });

  constructor() {
    this.reload();

    this.svc.listTypes().subscribe({
      next: (items) => {
        const list = (items ?? []).filter((x) => !!x?.key);
        if (list.length > 0) {
          this.types.set(list);
          const current = this.form.getRawValue().typeKey;
          if (!list.some((t) => t.key === current)) {
            this.form.patchValue({ typeKey: list[0].key });
          }
        }
      },
      error: () => {
        // keep fallback
      },
    });
  }

  reload() {
    this.loading.set(true);
    this.error.set(null);
    this.svc.list().subscribe({
      next: (items) => {
        this.loading.set(false);
        this.rooms.set(items ?? []);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudieron cargar los espacios.');
      },
    });
  }

  startEdit(room: Room) {
    this.editingId.set(room.id);
    this.saveError.set(null);
    this.form.patchValue({
      name: room.name,
      typeKey: room.typeKey,
      maxCapacity: room.maxCapacity,
      equipment: room.equipment ?? '',
    });
  }

  cancelEdit() {
    this.editingId.set(null);
    this.saveError.set(null);
    this.form.reset({
      name: '',
      typeKey: this.types()[0]?.key ?? FALLBACK_TYPES[0].key,
      maxCapacity: 1,
      equipment: '',
    });
  }

  save() {
    this.saveError.set(null);
    if (this.form.invalid) return;

    const payload = this.form.getRawValue();
    this.saving.set(true);

    const id = this.editingId();
    const call$ = id ? this.svc.update(id, payload) : this.svc.create(payload);

    call$.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelEdit();
        this.reload();
      },
      error: (err) => {
        this.saving.set(false);
        this.saveError.set(getHttpErrorMessage(err) ?? 'No se pudo guardar el espacio.');
      },
    });
  }

  remove(id: string) {
    this.deletingId.set(id);
    this.svc.delete(id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.reload();
      },
      error: (err) => {
        this.deletingId.set(null);
        this.error.set(getHttpErrorMessage(err) ?? 'No se pudo eliminar el espacio.');
      },
    });
  }
}
