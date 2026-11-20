import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogService, DialogConfig } from '../services/dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (isOpen()) {
      <div class="dialog-overlay" (click)="onCancel()">
        <div class="dialog-container" (click)="$event.stopPropagation()">
          <div class="dialog-header">
            <h2>{{ config?.title }}</h2>
            <button class="close-btn" (click)="onCancel()" aria-label="Cerrar">×</button>
          </div>

          <div class="dialog-body">
            <p>{{ config?.message }}</p>
          </div>

          <div class="dialog-footer">
            <button class="btn btn-secondary" (click)="onCancel()">
              {{ config?.cancelText || 'Cancelar' }}
            </button>
            <button
              class="btn btn-primary"
              [class.btn-danger]="config?.isDangerous"
              (click)="onConfirm()"
            >
              {{ config?.confirmText || 'Confirmar' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styleUrl: './confirm-dialog.component.css',
})
export class ConfirmDialogComponent implements OnInit {
  config: DialogConfig | null = null;

  get isOpen() {
    return this.dialogService.isDialogOpen;
  }

  constructor(private dialogService: DialogService) {}

  ngOnInit() {
    this.dialogService.getConfig().subscribe((config) => {
      this.config = config;
    });
  }

  onConfirm() {
    this.dialogService.confirm(true);
  }

  onCancel() {
    this.dialogService.confirm(false);
  }
}
