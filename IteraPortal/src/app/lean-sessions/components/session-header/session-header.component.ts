import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LeanSession, LeanParticipant, SessionStatus } from '../../models/lean-session.models';

@Component({
  selector: 'app-session-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './session-header.component.html',
  styleUrls: ['./session-header.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SessionHeaderComponent {
  @Input() session: LeanSession | null = null;
  @Input() participants: LeanParticipant[] = [];
  
  @Output() endSession = new EventEmitter<void>();
  @Output() exportSession = new EventEmitter<void>();
  
  getStatusBadgeClass(status: SessionStatus): string {
    switch (status) {
      case SessionStatus.Draft:
        return 'bg-secondary';
      case SessionStatus.InProgress:
        return 'bg-success';
      case SessionStatus.Completed:
        return 'bg-primary';
      case SessionStatus.Closed:
        return 'bg-dark';
      default:
        return 'bg-secondary';
    }
  }
  
  getActiveParticipants(): LeanParticipant[] {
    return this.participants.filter(p => p.isActive);
  }
  
  formatDate(date: Date | undefined): string {
    if (!date) return '';
    return new Date(date).toLocaleDateString('en-US', {
      weekday: 'short',
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }
}
