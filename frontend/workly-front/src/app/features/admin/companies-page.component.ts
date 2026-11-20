import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from './admin.service';
import { Company } from '../../core/models/company';
import { Area } from '../../core/models/area';
import { getHttpErrorMessage } from '../../core/utils/http-error';
import { DialogService } from '../../shared/services/dialog.service';

@Component({
  selector: 'app-admin-companies-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="stack">
      <div class="card stack">
        <div class="row" style="justify-content: space-between">
          <div>
            <h2 style="margin: 0">Empresas</h2>
            <p class="muted" style="margin: 0.25rem 0 0">
              Registro de empresas cliente.
            </p>
          </div>
          <button class="btn" id="companies-reload-btn" type="button" (click)="reload()" [disabled]="loading()">
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
                <th>Dirección</th>
                <th>Sitio web</th>
              </tr>
            </thead>
            <tbody>
              @for (c of companies(); track c.id) {
                <tr>
                  <td>{{ c.name }}</td>
                  <td class="muted">{{ formatAddress(c) }}</td>
                  <td>
                    <a [href]="c.website" target="_blank" rel="noopener">{{ c.website }}</a>
                  </td>
                </tr>
              }
              @if (companies().length === 0) {
                <tr>
                  <td colspan="3" class="muted">No hay empresas registradas.</td>
                </tr>
              }
            </tbody>
          </table>
        }
      </div>

      <div class="card stack">
        <h3 style="margin: 0">Registrar empresa</h3>

        <form class="stack" [formGroup]="form" (ngSubmit)="create()">
          <div class="grid-2">
            <div class="field">
              <label for="name">Nombre</label>
              <input id="name" formControlName="name" />
            </div>
            <div class="field">
              <label for="website">Sitio web (URL)</label>
              <input id="website" formControlName="website" />
            </div>
          </div>

          <div class="grid-2">
            <div class="field">
              <label for="street">Calle</label>
              <input id="street" formControlName="street" />
            </div>
            <div class="field">
              <label for="number">Número</label>
              <input id="number" type="number" formControlName="number" />
            </div>
          </div>

          <div class="grid-2">
            <div class="field">
              <label for="city">Ciudad</label>
              <input id="city" formControlName="city" />
            </div>
            <div class="field">
              <label for="state">Departamento/Estado</label>
              <input id="state" formControlName="state" />
            </div>
          </div>

          <div class="grid-2">
            <div class="field">
              <label for="zipCode">Código postal</label>
              <input id="zipCode" formControlName="zipCode" />
            </div>
            <div class="field">
              <label for="country">País</label>
              <input id="country" formControlName="country" />
            </div>
          </div>

          <div class="field">
            <label for="logo">Logo (archivo)</label>
            <input id="logo" type="file" accept="image/*" (change)="onLogoChange($event)" />
            @if (!logoFile()) {
              <span class="muted">Seleccioná un archivo de imagen (mandatorio).</span>
            }
          </div>

          @if (createError()) {
            <div class="error">{{ createError() }}</div>
          }

          <div class="row" style="justify-content: flex-end">
            <button
              class="btn btn-primary"
              type="submit"
              [disabled]="form.invalid || !logoFile() || creating()"
            >
              @if (creating()) {
                Creando...
              } @else {
                Crear empresa
              }
            </button>
          </div>
        </form>
      </div>

      <div class="card stack">
        <div class="row" style="justify-content: space-between">
          <div>
            <h3 style="margin: 0">Áreas por empresa</h3>
            <p class="muted" style="margin: 0.25rem 0 0">
              Administrá las áreas dentro del contexto de cada empresa.
            </p>
          </div>
          <button class="btn" type="button" (click)="reload()" [disabled]="loading()">
            Recargar datos
          </button>
        </div>

        <div class="field" style="max-width: 22rem">
          <label for="area-company">Empresa</label>
          <select id="area-company" [value]="selectedCompanyId()" (change)="onSelectedCompanyChange($event)">
            <option value="" disabled>Seleccionar...</option>
            @for (c of companies(); track c.id) {
              <option [value]="c.id">{{ c.name }}</option>
            }
          </select>
        </div>

        @if (!selectedCompany()) {
          <div class="muted">Seleccioná una empresa para ver y editar sus áreas.</div>
        } @else {
          <div class="grid-2">
            <div>
              <table class="table">
                <thead>
                  <tr>
                    <th>Nombre</th>
                    <th>Descripción</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  @for (area of selectedAreas(); track area.id) {
                    <tr>
                      <td>{{ area.name }}</td>
                      <td class="muted">{{ area.description }}</td>
                      <td style="text-align: right">
                        <div class="row" style="justify-content: flex-end">
                          <button class="btn" type="button" (click)="startEditArea(area)">Editar</button>
                          <button class="btn btn-danger" type="button" (click)="removeArea(area)">Eliminar</button>
                        </div>
                      </td>
                    </tr>
                  }
                  @if (selectedAreas().length === 0) {
                    <tr>
                      <td colspan="3" class="muted" style="text-align:center; padding:2rem">Sin áreas registradas.</td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>

            <div>
              <h4 style="margin: 0 0 0.75rem">
                @if (editingAreaId()) {
                  Editar área
                } @else {
                  Crear área
                }
              </h4>

              <form class="stack" [formGroup]="areaForm" (ngSubmit)="saveArea()">
                <div class="field">
                  <label for="area-name">Nombre</label>
                  <input id="area-name" formControlName="name" />
                </div>

                <div class="field">
                  <label for="area-description">Descripción</label>
                  <textarea id="area-description" rows="4" formControlName="description"></textarea>
                </div>

                @if (areaError()) {
                  <div class="error">{{ areaError() }}</div>
                }

                <div class="row" style="justify-content: flex-end">
                  @if (editingAreaId()) {
                    <button class="btn" type="button" (click)="cancelAreaEdit()">Cancelar</button>
                  }
                  <button class="btn btn-primary" type="submit" [disabled]="areaForm.invalid || savingArea()">
                    @if (savingArea()) {
                      Guardando...
                    } @else {
                      Guardar área
                    }
                  </button>
                </div>
              </form>
            </div>
          </div>
        }
      </div>
    </div>
  `,
})
export class AdminCompaniesPageComponent {
  private readonly admin = inject(AdminService);
  private readonly fb = inject(FormBuilder);
  private readonly dialog = inject(DialogService);

  companies = signal<Company[]>([]);
  loading = signal(false);
  error = signal<string | null>(null);

  creating = signal(false);
  createError = signal<string | null>(null);
  logoFile = signal<File | null>(null);

  selectedCompanyId = signal<string>('');
  editingAreaId = signal<string | null>(null);
  savingArea = signal(false);
  areaError = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    website: ['', [Validators.required]],
    street: ['', [Validators.required]],
    number: [1, [Validators.required, Validators.min(1)]],
    city: ['', [Validators.required]],
    state: ['', [Validators.required]],
    zipCode: ['', [Validators.required]],
    country: ['', [Validators.required]],
  });

  areaForm = this.fb.nonNullable.group({
    name: ['', [Validators.required]],
    description: ['', [Validators.required]],
  });

  selectedCompany = () => this.companies().find((company) => company.id === this.selectedCompanyId()) ?? null;
  selectedAreas = () => this.selectedCompany()?.areas ?? [];

  constructor() {
    this.reload();
  }

  reload() {
    this.loading.set(true);
    this.error.set(null);
    this.admin.listCompanies().subscribe({
      next: (items) => {
        this.loading.set(false);
        this.companies.set(items ?? []);
        if (!this.selectedCompanyId() && (items ?? []).length > 0) {
          this.selectedCompanyId.set(items![0].id);
        }
      },
      error: () => {
        this.loading.set(false);
        this.error.set('No se pudieron cargar las empresas.');
      },
    });
  }

  onLogoChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.logoFile.set(input.files?.item(0) ?? null);
  }

  create() {
    this.createError.set(null);
    if (this.form.invalid || !this.logoFile()) return;

    const { name, website, street, number, city, state, zipCode, country } =
      this.form.getRawValue();

    this.creating.set(true);

    fileToRawBase64(this.logoFile()!)
      .then((base64Logo) => {
        this.admin
          .createCompany({
            name,
            website,
            base64Logo,
            address: { street, number: Number(number), city, state, zipCode, country },
          })
          .subscribe({
            next: () => {
              this.creating.set(false);
              this.form.reset({
                name: '',
                website: '',
                street: '',
                number: 1,
                city: '',
                state: '',
                zipCode: '',
                country: '',
              });
              this.logoFile.set(null);
              this.reload();
            },
            error: (err) => {
              this.creating.set(false);
              this.createError.set(getHttpErrorMessage(err) ?? 'No se pudo crear la empresa.');
            },
          });
      })
      .catch(() => {
        this.creating.set(false);
        this.createError.set('No se pudo leer el logo en base64.');
      });
  }

  onSelectedCompanyChange(event: Event) {
    const select = event.target as HTMLSelectElement;
    this.selectedCompanyId.set(select.value);
    this.cancelAreaEdit();
  }

  startEditArea(area: Area) {
    this.editingAreaId.set(area.id);
    this.areaError.set(null);
    this.areaForm.patchValue({
      name: area.name,
      description: area.description,
    });
  }

  cancelAreaEdit() {
    this.editingAreaId.set(null);
    this.areaError.set(null);
    this.areaForm.reset({
      name: '',
      description: '',
    });
  }

  saveArea() {
    this.areaError.set(null);

    const companyId = this.selectedCompanyId();
    if (!companyId || this.areaForm.invalid) return;

    const payload = this.areaForm.getRawValue();
    this.savingArea.set(true);

    const areaId = this.editingAreaId();
    const call$ = areaId
      ? this.admin.updateArea(companyId, areaId, payload)
      : this.admin.createArea(companyId, payload);

    call$.subscribe({
      next: () => {
        this.savingArea.set(false);
        this.cancelAreaEdit();
        this.reload();
      },
      error: (err) => {
        this.savingArea.set(false);
        this.areaError.set(getHttpErrorMessage(err) ?? 'No se pudo guardar el área.');
      },
    });
  }

  removeArea(area: Area) {
    const companyId = this.selectedCompanyId();
    if (!companyId) return;
    this.dialog.openDialog({
      title: 'Eliminar área',
      message: `¿Estás seguro de que deseas eliminar el área "${area.name}"? Esta acción no se puede deshacer.`,
      confirmText: 'Eliminar',
      cancelText: 'Cancelar',
      isDangerous: true
    }).subscribe((result) => {
      if (!result.confirmed) return;

      this.areaError.set(null);
      this.admin.deleteArea(companyId, area.id).subscribe({
        next: () => {
          if (this.editingAreaId() === area.id) {
            this.cancelAreaEdit();
          }
          this.reload();
        },
        error: (err) => {
          const errorMsg = getHttpErrorMessage(err);
          // Mensaje más específico si hay invitaciones referenciando el área
          if (err.status === 409 || errorMsg?.includes('invitación') || errorMsg?.includes('referencia')) {
            this.areaError.set('No se puede eliminar el área porque hay invitaciones asignadas a ella.');
          } else {
            this.areaError.set(errorMsg ?? 'No se pudo eliminar el área.');
          }
        },
      });
    });
  }

  formatAddress(c: Company) {
    const a = c.address;
    return `${a.street} ${a.number}, ${a.city}, ${a.state}, ${a.zipCode}, ${a.country}`;
  }
}

function fileToRawBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onerror = () => reject(new Error('read error'));
    reader.onload = () => {
      const result = String(reader.result ?? '');
      const commaIndex = result.indexOf(',');
      if (commaIndex >= 0) {
        resolve(result.slice(commaIndex + 1));
      } else {
        resolve(result);
      }
    };
    reader.readAsDataURL(file);
  });
}
