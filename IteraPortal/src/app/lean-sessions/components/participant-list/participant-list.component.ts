import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LeanParticipant, ParticipantRole } from '../../models/lean-session.models';

@Component({
  selector: 'app-participant-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './participant-list.component.html',
  styleUrls: ['./participant-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ParticipantListComponent {
  @Input() participants: LeanParticipant[] = [];
  @Input() facilitatorId: string = '';
  
  getInitials(name: string): string {
    if (!name) return '?';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }
  
  isFacilitator(participant: LeanParticipant): boolean {
    return participant.role === ParticipantRole.Facilitator || 
           participant.userId === this.facilitatorId;
  }
  
  trackByUserId(index: number, participant: LeanParticipant): string {
    return participant.userId;
  }
}
