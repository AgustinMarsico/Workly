import { Injectable, signal } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface DialogConfig {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  isDangerous?: boolean;
}

export interface DialogResult {
  confirmed: boolean;
}

@Injectable({
  providedIn: 'root',
})
export class DialogService {
  private dialogConfig$ = new BehaviorSubject<DialogConfig | null>(null);
  private dialogResult$ = new BehaviorSubject<DialogResult | null>(null);

  isDialogOpen = signal(false);

  openDialog(config: DialogConfig): Observable<DialogResult> {
    this.dialogConfig$.next(config);
    this.isDialogOpen.set(true);

    return new Observable((observer) => {
      const subscription = this.dialogResult$.subscribe((result) => {
        if (result !== null) {
          observer.next(result);
          observer.complete();
          this.isDialogOpen.set(false);
          this.dialogResult$.next(null);
        }
      });

      return () => {
        subscription.unsubscribe();
      };
    });
  }

  confirm(result: boolean) {
    this.dialogResult$.next({ confirmed: result });
  }

  getConfig(): Observable<DialogConfig | null> {
    return this.dialogConfig$.asObservable();
  }
}
