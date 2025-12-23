import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ConfirmationDialogConfig {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  confirmButtonClass?: string;
}

@Component({
  selector: 'app-confirmation-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirmation-dialog.component.html',
  styleUrls: ['./confirmation-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmationDialogComponent {
  @Input({ required: true }) config!: ConfirmationDialogConfig;
  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();
  
  get title(): string {
    return this.config.title;
  }
  
  get message(): string {
    return this.config.message;
  }
  
  get confirmText(): string {
    return this.config.confirmText || 'Confirm';
  }
  
  get cancelText(): string {
    return this.config.cancelText || 'Cancel';
  }
  
  get confirmButtonClass(): string {
    return this.config.confirmButtonClass || 'btn-danger';
  }
  
  onConfirm(): void {
    this.confirmed.emit();
  }
  
  onCancel(): void {
    this.cancelled.emit();
  }
}
